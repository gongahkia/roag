// Static C# port of src/generation.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 84789ae646349c23b232757989d744b05539c8a79c7621e7f82448a9c450441d
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_generation { internal static Results Load(NativeRuntime rt) {
var values_1 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_2 = values_1[0];
var values_3 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_4 = values_3[0];
var values_5 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_6 = values_5[0];
var values_7 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.biomes"))));
V v_8 = values_7[0];
var values_9 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.world"))));
V v_10 = values_9[0];
var values_11 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.studio_world"))));
V v_12 = values_11[0];
var values_13 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.terrain"))));
V v_14 = values_13[0];
var values_15 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.corridor_branches"))));
V v_16 = values_15[0];
var values_17 = R.Join(new V[] {}, new Results(R.Table()));
V v_18 = values_17[0];
var values_19 = R.Join(new V[] {}, new Results((V)"core:map/overgrown_ruins"));
var obj_20 = v_18;
V key_21 = (V)"map_id";
R.Set(obj_20, key_21, values_19[0]);
var values_22 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"biome"), (V)"floor_id")));
var obj_23 = v_18;
V key_24 = (V)"floor_id";
R.Set(obj_23, key_24, values_22[0]);
var values_25 = R.Join(new V[] {}, new Results(R.Get(v_6, (V)"version")));
var obj_26 = v_18;
V key_27 = (V)"room_content_version";
R.Set(obj_26, key_27, values_25[0]);
var values_28 = R.Join(new V[] {R.Get(R.Get(v_6, (V)"biome"), (V)"width")}, new Results(R.Get(R.Get(v_6, (V)"biome"), (V)"height")));
var obj_29 = v_18;
V key_30 = (V)"width";
var obj_31 = v_18;
V key_32 = (V)"height";
R.Set(obj_29, key_30, values_28[0]);
R.Set(obj_31, key_32, values_28[1]);
var values_33 = R.Join(new V[] {R.Get(R.Get(v_6, (V)"biome"), (V)"room_min")}, new Results(R.Get(R.Get(v_6, (V)"biome"), (V)"room_max")));
var obj_34 = v_18;
V key_35 = (V)"room_min";
var obj_36 = v_18;
V key_37 = (V)"room_max";
R.Set(obj_34, key_35, values_33[0]);
R.Set(obj_36, key_37, values_33[1]);
var values_38 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_6, (V)"biome"), (V)"standard_chests")));
var obj_39 = v_18;
V key_40 = (V)"chest_target";
R.Set(obj_39, key_40, values_38[0]);
R.Set(v_18, (V)"biome_for_map", new V((RuleFunction)(args_41 => {
V v_42 = args_41[0];
return R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"for_map"), R.Join(new V[] {}, new Results(v_42))));
return Results.Empty;
})));
var values_43 = Results.Empty;
V v_44 = values_43[0];
V v_45 = V.Nil;
v_45 = new V((RuleFunction)(args_46 => {
V v_47 = args_46[0];
if (R.Truth((V)(!R.Truth(v_47)))) {
return R.Join(new V[] {}, new Results(V.Nil));
}
var values_48 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"encode"), R.Join(new V[] {}, new Results(v_47))));
V v_49 = values_48[0];
var values_50 = R.Join(new V[] {(V)1.0d}, new Results((V)0.0d));
V v_51 = values_50[0];
V v_52 = values_50[1];
{ double end_55 = R.Num((V)R.Length(v_49)), step_56 = R.Num((V)1.0d);
for (double i_54 = R.Num((V)1.0d); step_56 > 0 ? i_54 <= end_55 : i_54 >= end_55; i_54 += step_56) {
V v_53 = i_54;
var values_57 = R.Join(new V[] {}, new Results((V)((V)(v_51 + R.Invoke(v_49, "byte", R.Join(new V[] {}, new Results(v_53))).First) % (V)65521.0d)));
v_51 = values_57[0];

var values_58 = R.Join(new V[] {}, new Results((V)((V)(v_52 + v_51) % (V)65521.0d)));
v_52 = values_58[0];
}
}
return R.Join(new V[] {}, R.Call(R.Get(rt.Global("string"), (V)"format"), R.Join(new V[] {(V)"%08x:%d", (V)((V)(v_52 * (V)65536.0d) + v_51)}, new Results((V)R.Length(v_49)))));
return Results.Empty;
}));
R.Set(v_18, (V)"install_studio_pack", new V((RuleFunction)(args_59 => {
V v_60 = args_59[0];
if (R.Truth(v_60)) {
R.Call(R.Get(v_12, (V)"validate"), R.Join(new V[] {}, new Results(v_60)));
}
var values_61 = R.Join(new V[] {}, new Results(v_60));
v_44 = values_61[0];
return Results.Empty;
})));
R.Set(v_18, (V)"studio_pack_fingerprint", new V((RuleFunction)(args_62 => {

return R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {}, new Results(v_44))));
return Results.Empty;
})));
var values_63 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))))));
V v_64 = values_63[0];
V v_65 = V.Nil;
v_65 = new V((RuleFunction)(args_66 => {
V v_67 = args_66[0];
V v_68 = args_66[1];
return R.Join(new V[] {}, new Results(R.Concat(v_67, R.Concat((V)",", v_68))));
return Results.Empty;
}));
V v_69 = V.Nil;
v_69 = new V((RuleFunction)(args_70 => {
V v_71 = args_70[0];
V v_72 = args_70[1];
if (R.Truth((V)(v_71 > v_72))) {
var values_73 = R.Join(new V[] {v_72}, new Results(v_71));
v_71 = values_73[0];
v_72 = values_73[1];
}
return R.Join(new V[] {}, new Results(R.Concat(v_71, R.Concat((V)":", v_72))));
return Results.Empty;
}));
V v_74 = V.Nil;
v_74 = new V((RuleFunction)(args_75 => {
V v_76 = args_75[0];
V v_77 = args_75[1];
V v_78 = args_75[2];
V v_79 = args_75[3];
V v_80 = args_75[4];
var values_81 = R.Join(new V[] {}, R.Call(v_69, R.Join(new V[] {v_78}, new Results(v_79))));
V v_82 = values_81[0];
if (R.Truth(R.Get(v_77, v_82))) {
return R.Join(new V[] {}, new Results((V)false));
}
var values_83 = R.Join(new V[] {}, new Results((V)true));
var obj_84 = v_77;
V key_85 = v_82;
R.Set(obj_84, key_85, values_83[0]);
var values_86 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"a", v_78), new Entry((V)"b", v_79), new Entry((V)"kind", v_80), new Entry((V)"id", v_82))));
var obj_87 = R.Get(v_76, (V)"edges");
V key_88 = (V)((V)R.Length(R.Get(v_76, (V)"edges")) + (V)1.0d);
R.Set(obj_87, key_88, values_86[0]);
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
}));
R.Set(v_18, (V)"plan", new V((RuleFunction)(args_89 => {
V v_90 = args_89[0];
V v_91 = args_89[1];
V v_92 = args_89[2];
var values_93 = R.Join(new V[] {}, new Results(R.Or(v_92, () => R.Table(new Entry((V)"grid_cols", (V)4.0d), new Entry((V)"grid_rows", (V)3.0d), new Entry((V)"room_min", R.Get(v_18, (V)"room_min")), new Entry((V)"room_max", R.Get(v_18, (V)"room_max")), new Entry((V)"terminal_id", (V)12.0d)))));
v_92 = values_93[0];
var values_94 = R.Join(new V[] {R.Get(v_92, (V)"grid_cols")}, new Results(R.Get(v_92, (V)"grid_rows")));
V v_95 = values_94[0];
V v_96 = values_94[1];
V v_97 = V.Nil;
v_97 = new V((RuleFunction)(args_98 => {
V v_99 = args_98[0];
V v_100 = args_98[1];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_100 - (V)1.0d) * v_95) + v_99)));
return Results.Empty;
}));
V v_101 = V.Nil;
v_101 = new V((RuleFunction)(args_102 => {
V v_103 = args_102[0];
return R.Join(new V[] {}, new Results((V)((V)((V)(v_103 - (V)1.0d) % v_95) + (V)1.0d)));
return Results.Empty;
}));
V v_104 = V.Nil;
v_104 = new V((RuleFunction)(args_105 => {
V v_106 = args_105[0];
return R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)(v_106 - (V)1.0d) / v_95)))).First + (V)1.0d)));
return Results.Empty;
}));
V v_107 = V.Nil;
v_107 = new V((RuleFunction)(args_108 => {
V v_109 = args_108[0];
var values_110 = R.Join(new V[] {}, new Results(R.Table()));
V v_111 = values_110[0];
foreach (var row_112 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_64)))))) {
V v_113 = row_112[0];
V v_114 = row_112[1];
var values_115 = R.Join(new V[] {(V)(R.Call(v_101, R.Join(new V[] {}, new Results(v_109))).First + R.Get(v_114, (V)1.0d))}, new Results((V)(R.Call(v_104, R.Join(new V[] {}, new Results(v_109))).First + R.Get(v_114, (V)2.0d))));
V v_116 = values_115[0];
V v_117 = values_115[1];
if (R.Truth(R.And(R.And(R.And((V)(v_116 >= (V)1.0d), () => (V)(v_116 <= v_95)), () => (V)(v_117 >= (V)1.0d)), () => (V)(v_117 <= v_96)))) {
var values_118 = R.Join(new V[] {}, R.Call(v_97, R.Join(new V[] {v_116}, new Results(v_117))));
var obj_119 = v_111;
V key_120 = (V)((V)R.Length(v_111) + (V)1.0d);
R.Set(obj_119, key_120, values_118[0]);
}
}
R.Call(R.Get(rt.Global("table"), (V)"sort"), R.Join(new V[] {}, new Results(v_111)));
return R.Join(new V[] {}, new Results(v_111));
return Results.Empty;
}));
var values_121 = R.Join(new V[] {}, new Results((V)((V)(R.Get(v_92, (V)"room_min") + R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)((V)(R.Get(v_92, (V)"room_max") - R.Get(v_92, (V)"room_min")) + (V)1.0d)))).First) - (V)1.0d)));
V v_122 = values_121[0];
var values_123 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"nodes", R.Table()), new Entry((V)"edges", R.Table()), new Entry((V)"critical", R.Table(new Entry((V)1, (V)1.0d))))));
V v_124 = values_123[0];
var values_125 = R.Join(new V[] {(V)1.0d}, new Results((V)1.0d));
V v_126 = values_125[0];
V v_127 = values_125[1];
if (R.Truth(R.And(R.And(R.And(v_91, () => (V)(R.Get(v_91, (V)"strategy") == (V)"courtyards")), () => (V)(v_95 == (V)4.0d)), () => (V)(v_96 == (V)3.0d)))) {
var values_128 = R.Join(new V[] {(V)2.0d}, new Results((V)2.0d));
v_126 = values_128[0];
v_127 = values_128[1];
var values_129 = R.Join(new V[] {}, R.Call(v_97, R.Join(new V[] {(V)2.0d}, new Results((V)1.0d))));
var obj_130 = R.Get(v_124, (V)"critical");
V key_131 = (V)2.0d;
R.Set(obj_130, key_131, values_129[0]);
var values_132 = R.Join(new V[] {}, R.Call(v_97, R.Join(new V[] {(V)2.0d}, new Results((V)2.0d))));
var obj_133 = R.Get(v_124, (V)"critical");
V key_134 = (V)3.0d;
R.Set(obj_133, key_134, values_132[0]);
}
while (R.Truth(R.Or((V)(v_126 < v_95), () => (V)(v_127 < v_96)))) {
if (R.Truth((V)(v_126 == v_95))) {
var values_135 = R.Join(new V[] {}, new Results((V)(v_127 + (V)1.0d)));
v_127 = values_135[0];
} else if (R.Truth((V)(v_127 == v_96))) {
var values_136 = R.Join(new V[] {}, new Results((V)(v_126 + (V)1.0d)));
v_126 = values_136[0];
} else if (R.Truth((V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)2.0d))).First == (V)1.0d))) {
var values_137 = R.Join(new V[] {}, new Results((V)(v_126 + (V)1.0d)));
v_126 = values_137[0];
} else {
var values_138 = R.Join(new V[] {}, new Results((V)(v_127 + (V)1.0d)));
v_127 = values_138[0];
}
var values_139 = R.Join(new V[] {}, R.Call(v_97, R.Join(new V[] {v_126}, new Results(v_127))));
V v_140 = values_139[0];
var values_141 = R.Join(new V[] {}, new Results(v_140));
var obj_142 = R.Get(v_124, (V)"critical");
V key_143 = (V)((V)R.Length(R.Get(v_124, (V)"critical")) + (V)1.0d);
R.Set(obj_142, key_143, values_141[0]);
}
if (R.Truth(R.And((V)(v_122 >= (V)10.0d), () => (V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)2.0d))).First == (V)1.0d)))) {
var values_144 = R.Join(new V[] {}, new Results(R.Table()));
V v_145 = values_144[0];
foreach (var row_146 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_124, (V)"critical"))))))) {
V v_147 = row_146[0];
V v_148 = row_146[1];
var values_149 = R.Join(new V[] {}, new Results((V)true));
var obj_150 = v_145;
V key_151 = v_148;
R.Set(obj_150, key_151, values_149[0]);
}
var values_152 = R.Join(new V[] {}, new Results(R.Table()));
V v_153 = values_152[0];
{ double end_156 = R.Num((V)((V)R.Length(R.Get(v_124, (V)"critical")) - (V)1.0d)), step_157 = R.Num((V)1.0d);
for (double i_155 = R.Num((V)1.0d); step_157 > 0 ? i_155 <= end_156 : i_155 >= end_156; i_155 += step_157) {
V v_154 = i_155;
var values_158 = R.Join(new V[] {R.Get(R.Get(v_124, (V)"critical"), v_154)}, new Results(R.Get(R.Get(v_124, (V)"critical"), (V)(v_154 + (V)1.0d))));
V v_159 = values_158[0];
V v_160 = values_158[1];
foreach (var row_161 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d)))))))) {
V v_162 = row_161[0];
V v_163 = row_161[1];
var values_164 = Results.Empty;
V v_165 = values_164[0];
V v_166 = values_164[1];
if (R.Truth((V)(R.Call(v_104, R.Join(new V[] {}, new Results(v_159))).First == R.Call(v_104, R.Join(new V[] {}, new Results(v_160))).First))) {
var values_167 = R.Join(new V[] {}, new Results((V)(R.Call(v_104, R.Join(new V[] {}, new Results(v_159))).First + v_163)));
V v_168 = values_167[0];
if (R.Truth(R.And((V)(v_168 >= (V)1.0d), () => (V)(v_168 <= v_96)))) {
var values_169 = R.Join(new V[] {R.Call(v_97, R.Join(new V[] {R.Call(v_101, R.Join(new V[] {}, new Results(v_159))).First}, new Results(v_168))).First}, R.Call(v_97, R.Join(new V[] {R.Call(v_101, R.Join(new V[] {}, new Results(v_160))).First}, new Results(v_168))));
v_165 = values_169[0];
v_166 = values_169[1];
}
} else {
var values_170 = R.Join(new V[] {}, new Results((V)(R.Call(v_101, R.Join(new V[] {}, new Results(v_159))).First + v_163)));
V v_171 = values_170[0];
if (R.Truth(R.And((V)(v_171 >= (V)1.0d), () => (V)(v_171 <= v_95)))) {
var values_172 = R.Join(new V[] {R.Call(v_97, R.Join(new V[] {v_171}, R.Call(v_104, R.Join(new V[] {}, new Results(v_159))))).First}, R.Call(v_97, R.Join(new V[] {v_171}, R.Call(v_104, R.Join(new V[] {}, new Results(v_160))))));
v_165 = values_172[0];
v_166 = values_172[1];
}
}
if (R.Truth(R.And(R.And(v_165, () => (V)(!R.Truth(R.Get(v_145, v_165)))), () => (V)(!R.Truth(R.Get(v_145, v_166)))))) {
var values_173 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"index", v_154), new Entry((V)"p", v_165), new Entry((V)"q", v_166))));
var obj_174 = v_153;
V key_175 = (V)((V)R.Length(v_153) + (V)1.0d);
R.Set(obj_174, key_175, values_173[0]);
}
}
}
}
if (R.Truth((V)((V)R.Length(v_153) > (V)0.0d))) {
var values_176 = R.Join(new V[] {}, new Results(R.Get(v_153, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)R.Length(v_153)))).First)));
V v_177 = values_176[0];
R.Call(R.Get(rt.Global("table"), (V)"insert"), R.Join(new V[] {R.Get(v_124, (V)"critical"), (V)(R.Get(v_177, (V)"index") + (V)1.0d)}, new Results(R.Get(v_177, (V)"p"))));
R.Call(R.Get(rt.Global("table"), (V)"insert"), R.Join(new V[] {R.Get(v_124, (V)"critical"), (V)(R.Get(v_177, (V)"index") + (V)2.0d)}, new Results(R.Get(v_177, (V)"q"))));
}
}
var values_178 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_179 = values_178[0];
V v_180 = values_178[1];
foreach (var row_181 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_124, (V)"critical"))))))) {
V v_182 = row_181[0];
V v_183 = row_181[1];
var values_184 = R.Join(new V[] {}, new Results((V)true));
var obj_185 = v_179;
V key_186 = v_183;
R.Set(obj_185, key_186, values_184[0]);
if (R.Truth((V)(v_182 > (V)1.0d))) {
R.Call(v_74, R.Join(new V[] {v_124, v_180, R.Get(R.Get(v_124, (V)"critical"), (V)(v_182 - (V)1.0d)), v_183}, new Results((V)"route")));
}
}
var values_187 = R.Join(new V[] {}, new Results((V)R.Length(R.Get(v_124, (V)"critical"))));
V v_188 = values_187[0];
while (R.Truth((V)(v_188 < v_122))) {
var values_189 = R.Join(new V[] {}, new Results(R.Table()));
V v_190 = values_189[0];
{ double end_193 = R.Num((V)(v_95 * v_96)), step_194 = R.Num((V)1.0d);
for (double i_192 = R.Num((V)1.0d); step_194 > 0 ? i_192 <= end_193 : i_192 >= end_193; i_192 += step_194) {
V v_191 = i_192;
if (R.Truth((V)(!R.Truth(R.Get(v_179, v_191))))) {
foreach (var row_195 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_107, R.Join(new V[] {}, new Results(v_191)))))))) {
V v_196 = row_195[0];
V v_197 = row_195[1];
if (R.Truth(R.Get(v_179, v_197))) {
var values_198 = R.Join(new V[] {}, new Results(v_191));
var obj_199 = v_190;
V key_200 = (V)((V)R.Length(v_190) + (V)1.0d);
R.Set(obj_199, key_200, values_198[0]);

break;
}
}
}
}
}
var values_201 = R.Join(new V[] {}, new Results(R.Get(v_190, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)R.Length(v_190)))).First)));
V v_202 = values_201[0];
var values_203 = R.Join(new V[] {}, new Results(R.Table()));
V v_204 = values_203[0];
foreach (var row_205 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_107, R.Join(new V[] {}, new Results(v_202)))))))) {
V v_206 = row_205[0];
V v_207 = row_205[1];
if (R.Truth(R.Get(v_179, v_207))) {
var values_208 = R.Join(new V[] {}, new Results(v_207));
var obj_209 = v_204;
V key_210 = (V)((V)R.Length(v_204) + (V)1.0d);
R.Set(obj_209, key_210, values_208[0]);
}
}
var values_211 = R.Join(new V[] {}, new Results((V)true));
var obj_212 = v_179;
V key_213 = v_202;
R.Set(obj_212, key_213, values_211[0]);
var values_214 = R.Join(new V[] {}, new Results((V)(v_188 + (V)1.0d)));
v_188 = values_214[0];
R.Call(v_74, R.Join(new V[] {v_124, v_180, v_202, R.Get(v_204, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)R.Length(v_204)))).First)}, new Results((V)"branch")));
}
var values_215 = R.Join(new V[] {}, new Results(R.Table()));
V v_216 = values_215[0];
{ double end_219 = R.Num((V)(v_95 * v_96)), step_220 = R.Num((V)1.0d);
for (double i_218 = R.Num((V)1.0d); step_220 > 0 ? i_218 <= end_219 : i_218 >= end_219; i_218 += step_220) {
V v_217 = i_218;
if (R.Truth(R.Get(v_179, v_217))) {
foreach (var row_221 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, R.Call(v_107, R.Join(new V[] {}, new Results(v_217)))))))) {
V v_222 = row_221[0];
V v_223 = row_221[1];
if (R.Truth(R.And(R.And(R.Get(v_179, v_223), () => (V)(v_217 < v_223)), () => (V)(!R.Truth(R.Get(v_180, R.Call(v_69, R.Join(new V[] {v_217}, new Results(v_223))).First)))))) {
var values_224 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, v_217), new Entry((V)2, v_223))));
var obj_225 = v_216;
V key_226 = (V)((V)R.Length(v_216) + (V)1.0d);
R.Set(obj_225, key_226, values_224[0]);
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_216) > (V)0.0d)}, new Results((V)"room plan has no loop candidate")));
var values_227 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"remove"), R.Join(new V[] {v_216}, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)R.Length(v_216)))))));
V v_228 = values_227[0];
R.Call(v_74, R.Join(new V[] {v_124, v_180, R.Get(v_228, (V)1.0d), R.Get(v_228, (V)2.0d)}, new Results((V)"loop")));
if (R.Truth(R.And((V)((V)R.Length(v_216) > (V)0.0d), () => R.Or(R.And(v_91, () => R.Or(R.Or((V)(R.Get(v_91, (V)"strategy") == (V)"canals"), () => (V)(R.Get(v_91, (V)"strategy") == (V)"observatory")), () => (V)(R.Get(v_91, (V)"strategy") == (V)"forge"))), () => (V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)3.0d))).First == (V)1.0d))))) {
var values_229 = R.Join(new V[] {}, new Results(R.Get(v_216, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_90}, new Results((V)R.Length(v_216)))).First)));
V v_230 = values_229[0];
R.Call(v_74, R.Join(new V[] {v_124, v_180, R.Get(v_230, (V)1.0d), R.Get(v_230, (V)2.0d)}, new Results((V)"loop")));
}
{ double end_233 = R.Num((V)(v_95 * v_96)), step_234 = R.Num((V)1.0d);
for (double i_232 = R.Num((V)1.0d); step_234 > 0 ? i_232 <= end_233 : i_232 >= end_233; i_232 += step_234) {
V v_231 = i_232;
if (R.Truth(R.Get(v_179, v_231))) {
var values_235 = R.Join(new V[] {}, new Results((V)"combat"));
V v_236 = values_235[0];
if (R.Truth((V)(v_231 == (V)1.0d))) {
var values_237 = R.Join(new V[] {}, new Results((V)"start"));
v_236 = values_237[0];
} else if (R.Truth((V)(v_231 == R.Get(v_92, (V)"terminal_id")))) {
var values_238 = R.Join(new V[] {}, new Results((V)"elite"));
v_236 = values_238[0];
} else if (R.Truth(R.Or((V)((V)(v_231 % v_95) == (V)0.0d), () => (V)((V)(v_231 % (V)5.0d) == (V)0.0d)))) {
var values_239 = R.Join(new V[] {}, new Results((V)"treasure"));
v_236 = values_239[0];
} else if (R.Truth((V)((V)(v_231 % (V)3.0d) == (V)0.0d))) {
var values_240 = R.Join(new V[] {}, new Results((V)"landmark"));
v_236 = values_240[0];
}
var values_241 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_231), new Entry((V)"col", R.Call(v_101, R.Join(new V[] {}, new Results(v_231))).First), new Entry((V)"row", R.Call(v_104, R.Join(new V[] {}, new Results(v_231))).First), new Entry((V)"role", v_236))));
var obj_242 = R.Get(v_124, (V)"nodes");
V key_243 = (V)((V)R.Length(R.Get(v_124, (V)"nodes")) + (V)1.0d);
R.Set(obj_242, key_243, values_241[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_124));
return Results.Empty;
})));
V v_244 = V.Nil;
v_244 = new V((RuleFunction)(args_245 => {
V v_246 = args_245[0];
V v_247 = args_245[1];
V v_248 = args_245[2];
var values_249 = R.Join(new V[] {}, new Results(R.Table()));
V v_250 = values_249[0];
{ double end_253 = R.Num(v_247), step_254 = R.Num((V)1.0d);
for (double i_252 = R.Num((V)1.0d); step_254 > 0 ? i_252 <= end_253 : i_252 >= end_253; i_252 += step_254) {
V v_251 = i_252;
var values_255 = R.Join(new V[] {}, new Results(R.Table()));
var obj_256 = v_250;
V key_257 = v_251;
R.Set(obj_256, key_257, values_255[0]);
{ double end_260 = R.Num(v_246), step_261 = R.Num((V)1.0d);
for (double i_259 = R.Num((V)1.0d); step_261 > 0 ? i_259 <= end_260 : i_259 >= end_260; i_259 += step_261) {
V v_258 = i_259;
var values_262 = R.Join(new V[] {}, new Results(v_248));
var obj_263 = R.Get(v_250, v_251);
V key_264 = v_258;
R.Set(obj_263, key_264, values_262[0]);
}
}
}
}
return R.Join(new V[] {}, new Results(v_250));
return Results.Empty;
}));
V v_265 = V.Nil;
v_265 = new V((RuleFunction)(args_266 => {
V v_267 = args_266[0];
var values_268 = R.Join(new V[] {}, new Results(R.Table()));
V v_269 = values_268[0];
{ double end_272 = R.Num((V)R.Length(v_267)), step_273 = R.Num((V)1.0d);
for (double i_271 = R.Num((V)1.0d); step_273 > 0 ? i_271 <= end_272 : i_271 >= end_272; i_271 += step_273) {
V v_270 = i_271;
var values_274 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("table"), (V)"concat"), R.Join(new V[] {}, new Results(R.Get(v_267, v_270)))));
var obj_275 = v_269;
V key_276 = v_270;
R.Set(obj_275, key_276, values_274[0]);
}
}
return R.Join(new V[] {}, new Results(v_269));
return Results.Empty;
}));
V v_277 = V.Nil;
v_277 = new V((RuleFunction)(args_278 => {
V v_279 = args_278[0];
V v_280 = args_278[1];
V v_281 = args_278[2];
V v_282 = args_278[3];
V v_283 = args_278[4];
var values_284 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_279, (V)"rows"), v_281)));
V v_285 = values_284[0];
var values_286 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_285, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_280 - (V)1.0d)))).First, R.Concat(v_282, R.Invoke(v_285, "sub", R.Join(new V[] {}, new Results((V)(v_280 + (V)1.0d)))).First))));
var obj_287 = R.Get(v_279, (V)"rows");
V key_288 = v_281;
R.Set(obj_287, key_288, values_286[0]);
if (R.Truth(v_283)) {
var values_289 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_279, (V)"visual_rows"), v_281)));
V v_290 = values_289[0];
var values_291 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_290, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(v_280 - (V)1.0d)))).First, R.Concat(v_283, R.Invoke(v_290, "sub", R.Join(new V[] {}, new Results((V)(v_280 + (V)1.0d)))).First))));
var obj_292 = R.Get(v_279, (V)"visual_rows");
V key_293 = v_281;
R.Set(obj_292, key_293, values_291[0]);
}
return Results.Empty;
}));
var values_294 = R.Join(new V[] {}, new Results(v_277));
var obj_295 = v_18;
V key_296 = (V)"set_tile";
R.Set(obj_295, key_296, values_294[0]);
V v_297 = V.Nil;
v_297 = new V((RuleFunction)(args_298 => {
V v_299 = args_298[0];
V v_300 = args_298[1];
V v_301 = args_298[2];
var values_302 = R.Join(new V[] {}, new Results(R.Table()));
V v_303 = values_302[0];
foreach (var row_304 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_299, () => R.Table()))))))) {
V v_305 = row_304[0];
V v_306 = row_304[1];
var values_307 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)((V)(v_300 + R.Get(v_306, (V)"x")) - (V)1.0d)), new Entry((V)"y", (V)((V)(v_301 + R.Get(v_306, (V)"y")) - (V)1.0d)))));
var obj_308 = v_303;
V key_309 = (V)((V)R.Length(v_303) + (V)1.0d);
R.Set(obj_308, key_309, values_307[0]);
}
return R.Join(new V[] {}, new Results(v_303));
return Results.Empty;
}));
V v_310 = V.Nil;
v_310 = new V((RuleFunction)(args_311 => {
V v_312 = args_311[0];
V v_313 = args_311[1];
foreach (var row_314 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_312, (V)"sockets"))))))) {
V v_315 = row_314[0];
V v_316 = row_314[1];
if (R.Truth((V)(R.Get(v_316, (V)"facing") == v_313))) {
return R.Join(new V[] {}, new Results(v_316));
}
}
R.Call(rt.Global("error"), R.Join(new V[] {}, new Results(R.Concat((V)"missing room socket ", v_313))));
return Results.Empty;
}));
V v_317 = V.Nil;
v_317 = new V((RuleFunction)(args_318 => {
V v_319 = args_318[0];
V v_320 = args_318[1];
V v_321 = args_318[2];
V v_322 = args_318[3];
V v_323 = args_318[4];
V v_324 = args_318[5];
V v_325 = args_318[6];
V v_326 = args_318[7];
var values_327 = R.Join(new V[] {}, new Results(R.Get(v_319, (V)"layout")));
V v_328 = values_327[0];
var values_329 = R.Join(new V[] {}, new Results((V)((V)((V)(R.Or(R.And(v_328, () => R.Get(v_328, (V)"base_x")), () => (V)20.0d) + (V)((V)(R.Get(v_324, (V)"col") - (V)1.0d) * R.Or(R.And(v_328, () => R.Get(v_328, (V)"slot_x")), () => (V)32.0d))) + R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_323}, new Results((V)7.0d))).First) - (V)4.0d)));
V v_330 = values_329[0];
var values_331 = R.Join(new V[] {}, new Results((V)((V)((V)(R.Or(R.And(v_328, () => R.Get(v_328, (V)"base_y")), () => (V)16.0d) + (V)((V)(R.Get(v_324, (V)"row") - (V)1.0d) * R.Or(R.And(v_328, () => R.Get(v_328, (V)"slot_y")), () => (V)26.0d))) + R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_323}, new Results((V)7.0d))).First) - (V)4.0d)));
V v_332 = values_331[0];
var values_333 = R.Join(new V[] {}, new Results((V)(v_330 - R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_325, (V)"width") / (V)2.0d)))).First)));
V v_334 = values_333[0];
var values_335 = R.Join(new V[] {}, new Results((V)(v_332 - R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(R.Get(v_325, (V)"height") / (V)2.0d)))).First)));
V v_336 = values_335[0];
var values_337 = R.Join(new V[] {}, new Results(R.Table()));
V v_338 = values_337[0];
foreach (var row_339 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_325, (V)"editable"))))))) {
V v_340 = row_339[0];
V v_341 = row_339[1];
var values_342 = R.Join(new V[] {}, new Results((V)true));
var obj_343 = v_338;
V key_344 = R.Call(v_65, R.Join(new V[] {R.Get(v_341, (V)"x")}, new Results(R.Get(v_341, (V)"y")))).First;
R.Set(obj_343, key_344, values_342[0]);
}
var values_345 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_324, (V)"id")), new Entry((V)"slot", R.Get(v_324, (V)"id")), new Entry((V)"role", R.Get(v_324, (V)"role")), new Entry((V)"template_id", R.Get(v_325, (V)"id")), new Entry((V)"turns", R.Get(v_325, (V)"turns")), new Entry((V)"mirror", R.Get(v_325, (V)"mirror")), new Entry((V)"x", v_334), new Entry((V)"y", v_336), new Entry((V)"width", R.Get(v_325, (V)"width")), new Entry((V)"height", R.Get(v_325, (V)"height")), new Entry((V)"center", R.Table(new Entry((V)"x", v_330), new Entry((V)"y", v_332))), new Entry((V)"sockets", R.Table()), new Entry((V)"markers", R.Table()), new Entry((V)"structures", R.Table()))));
V v_346 = values_345[0];
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_325, (V)"markers"))))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
var values_350 = R.Join(new V[] {}, R.Call(v_297, R.Join(new V[] {v_349, v_334}, new Results(v_336))));
var obj_351 = R.Get(v_346, (V)"markers");
V key_352 = v_348;
R.Set(obj_351, key_352, values_350[0]);
}
foreach (var row_353 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_325, (V)"structures"), () => R.Table()))))))) {
V v_354 = row_353[0];
V v_355 = row_353[1];
var values_356 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)((V)(v_334 + R.Get(v_355, (V)"x")) - (V)1.0d)), new Entry((V)"y", (V)((V)(v_336 + R.Get(v_355, (V)"y")) - (V)1.0d)), new Entry((V)"kind", R.Get(v_355, (V)"kind")))));
var obj_357 = R.Get(v_346, (V)"structures");
V key_358 = (V)((V)R.Length(R.Get(v_346, (V)"structures")) + (V)1.0d);
R.Set(obj_357, key_358, values_356[0]);
}
foreach (var row_359 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_325, (V)"sockets"))))))) {
V v_360 = row_359[0];
V v_361 = row_359[1];
var values_362 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)((V)(v_334 + R.Get(v_361, (V)"x")) - (V)1.0d)), new Entry((V)"y", (V)((V)(v_336 + R.Get(v_361, (V)"y")) - (V)1.0d)), new Entry((V)"facing", R.Get(v_361, (V)"facing")), new Entry((V)"width", R.Get(v_361, (V)"width")))));
var obj_363 = R.Get(v_346, (V)"sockets");
V key_364 = (V)((V)R.Length(R.Get(v_346, (V)"sockets")) + (V)1.0d);
R.Set(obj_363, key_364, values_362[0]);
}
{ double end_367 = R.Num(R.Get(v_325, (V)"height")), step_368 = R.Num((V)1.0d);
for (double i_366 = R.Num((V)1.0d); step_368 > 0 ? i_366 <= end_367 : i_366 >= end_367; i_366 += step_368) {
V v_365 = i_366;
{ double end_371 = R.Num(R.Get(v_325, (V)"width")), step_372 = R.Num((V)1.0d);
for (double i_370 = R.Num((V)1.0d); step_372 > 0 ? i_370 <= end_371 : i_370 >= end_371; i_370 += step_372) {
V v_369 = i_370;
var values_373 = R.Join(new V[] {(V)((V)(v_334 + v_369) - (V)1.0d)}, new Results((V)((V)(v_336 + v_365) - (V)1.0d)));
V v_374 = values_373[0];
V v_375 = values_373[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(v_374 > (V)1.0d), () => (V)(v_374 < R.Get(v_319, (V)"width"))), () => (V)(v_375 > (V)1.0d)), () => (V)(v_375 < R.Get(v_319, (V)"height")))}, new Results((V)"room outside generated floor")));
var values_376 = R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_325, (V)"rows"), v_365), "sub", R.Join(new V[] {v_369}, new Results(v_369))));
V v_377 = values_376[0];
if (R.Truth((V)(v_377 != (V)" "))) {
R.Call(rt.Global("assert"), R.Join(new V[] {(V)(!R.Truth(R.Get(v_322, R.Call(v_65, R.Join(new V[] {v_374}, new Results(v_375))).First)))}, new Results((V)"room footprints overlap")));
var values_378 = R.Join(new V[] {}, new Results(R.Get(v_346, (V)"id")));
var obj_379 = v_322;
V key_380 = R.Call(v_65, R.Join(new V[] {v_374}, new Results(v_375))).First;
R.Set(obj_379, key_380, values_378[0]);
if (R.Truth((V)(v_377 == (V)"B"))) {
var values_381 = R.Join(new V[] {}, new Results((V)true));
var obj_382 = R.Get(v_319, (V)"breakable_origins");
V key_383 = R.Call(v_65, R.Join(new V[] {v_374}, new Results(v_375))).First;
R.Set(obj_382, key_383, values_381[0]);
}
var values_384 = R.Join(new V[] {}, new Results(v_377));
V v_385 = values_384[0];
if (R.Truth(R.Get(v_338, R.Call(v_65, R.Join(new V[] {v_369}, new Results(v_365))).First))) {
var values_386 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_374), new Entry((V)"y", v_375), new Entry((V)"kind", (V)"room"))));
var obj_387 = R.Get(v_319, (V)"editable_cells");
V key_388 = (V)((V)R.Length(R.Get(v_319, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_387, key_388, values_386[0]);
if (R.Truth((V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_323}, new Results((V)100.0d))).First <= (V)35.0d))) {
if (R.Truth((V)(R.Get(v_326, (V)"strategy") == (V)"canals"))) {
var values_389 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_377 == (V)"~"), () => (V)"."), () => (V)"~")));
v_385 = values_389[0];
} else {
var values_390 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_377 == (V)"#"), () => (V)"."), () => (V)"#")));
v_385 = values_390[0];
}
}
} else {
var values_391 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_374), new Entry((V)"y", v_375), new Entry((V)"tile", v_377))));
var obj_392 = R.Get(v_319, (V)"protected_cells");
V key_393 = (V)((V)R.Length(R.Get(v_319, (V)"protected_cells")) + (V)1.0d);
R.Set(obj_392, key_393, values_391[0]);
}
var values_394 = R.Join(new V[] {}, new Results(v_385));
var obj_395 = R.Get(v_320, v_375);
V key_396 = v_374;
R.Set(obj_395, key_396, values_394[0]);
var values_397 = R.Join(new V[] {}, new Results(R.And(R.Get(v_325, (V)"studio_visual_cells"), () => R.Get(R.Get(v_325, (V)"studio_visual_cells"), R.Call(v_65, R.Join(new V[] {v_369}, new Results(v_365))).First))));
V v_398 = values_397[0];
if (R.Truth(v_398)) {
var values_399 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_319, (V)"studio_visuals"), () => R.Table())));
var obj_400 = v_319;
V key_401 = (V)"studio_visuals";
R.Set(obj_400, key_401, values_399[0]);
var values_402 = R.Join(new V[] {}, new Results(v_398));
var obj_403 = R.Get(v_319, (V)"studio_visuals");
V key_404 = R.Call(v_65, R.Join(new V[] {v_374}, new Results(v_375))).First;
R.Set(obj_403, key_404, values_402[0]);
}
if (R.Truth((V)(v_385 == (V)"#"))) {
var values_405 = R.Join(new V[] {}, new Results(R.Get(v_326, (V)"wall_visual")));
var obj_406 = R.Get(v_321, v_375);
V key_407 = v_374;
R.Set(obj_406, key_407, values_405[0]);
} else if (R.Truth((V)(v_385 == (V)"~"))) {
var values_408 = R.Join(new V[] {}, new Results((V)"q"));
var obj_409 = R.Get(v_321, v_375);
V key_410 = v_374;
R.Set(obj_409, key_410, values_408[0]);
} else if (R.Truth((V)(R.Get(v_324, (V)"role") == (V)"elite"))) {
var values_411 = R.Join(new V[] {}, new Results((V)"a"));
var obj_412 = R.Get(v_321, v_375);
V key_413 = v_374;
R.Set(obj_412, key_413, values_411[0]);
} else {
var values_414 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_323}, new Results((V)12.0d))));
V v_415 = values_414[0];
if (R.Truth((V)(R.Get(v_326, (V)"strategy") == (V)"ruins"))) {
var values_416 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.Or(R.And((V)(v_415 == (V)1.0d), () => (V)"v"), () => R.And((V)(v_415 == (V)2.0d), () => (V)"r")), () => R.And((V)(v_415 <= (V)5.0d), () => (V)"m")), () => (V)"s")));
var obj_417 = R.Get(v_321, v_375);
V key_418 = v_374;
R.Set(obj_417, key_418, values_416[0]);
} else {
var values_419 = R.Join(new V[] {}, new Results(R.Or(R.Or(R.And((V)(v_415 <= (V)3.0d), () => R.Get(v_326, (V)"floor_visual")), () => R.And((V)(v_415 == (V)4.0d), () => (V)"r")), () => R.Get(v_326, (V)"floor_visual"))));
var obj_420 = R.Get(v_321, v_375);
V key_421 = v_374;
R.Set(obj_420, key_421, values_419[0]);
}
}
}
}
}
}
}
return R.Join(new V[] {}, new Results(v_346));
return Results.Empty;
}));
V v_422 = V.Nil;
v_422 = new V((RuleFunction)(args_423 => {
V v_424 = args_423[0];
V v_425 = args_423[1];
V v_426 = args_423[2];
V v_427 = args_423[3];
var values_428 = R.Join(new V[] {}, new Results(R.Table()));
V v_429 = values_428[0];
V v_430 = V.Nil;
v_430 = new V((RuleFunction)(args_431 => {
V v_432 = args_431[0];
V v_433 = args_431[1];
V v_434 = args_431[2];
V v_435 = args_431[3];
var values_436 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_432 == v_434), () => (V)0.0d), () => R.Or(R.And((V)(v_434 > v_432), () => (V)1.0d), () => (-(V)1.0d)))));
V v_437 = values_436[0];
var values_438 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(v_433 == v_435), () => (V)0.0d), () => R.Or(R.And((V)(v_435 > v_433), () => (V)1.0d), () => (-(V)1.0d)))));
V v_439 = values_438[0];
var values_440 = R.Join(new V[] {v_432}, new Results(v_433));
V v_441 = values_440[0];
V v_442 = values_440[1];
while (R.Truth((V)true)) {
if (R.Truth(R.Or(R.Or((V)((V)R.Length(v_429) == (V)0.0d), () => (V)(R.Get(R.Get(v_429, (V)R.Length(v_429)), (V)"x") != v_441)), () => (V)(R.Get(R.Get(v_429, (V)R.Length(v_429)), (V)"y") != v_442)))) {
var values_443 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_441), new Entry((V)"y", v_442))));
var obj_444 = v_429;
V key_445 = (V)((V)R.Length(v_429) + (V)1.0d);
R.Set(obj_444, key_445, values_443[0]);
}
if (R.Truth(R.And((V)(v_441 == v_434), () => (V)(v_442 == v_435)))) {
break;
}
var values_446 = R.Join(new V[] {(V)(v_441 + v_437)}, new Results((V)(v_442 + v_439)));
v_441 = values_446[0];
v_442 = values_446[1];
}
return Results.Empty;
}));
if (R.Truth(R.Or((V)(R.Get(v_424, (V)"facing") == (V)"E"), () => (V)(R.Get(v_424, (V)"facing") == (V)"W")))) {
var values_447 = R.Join(new V[] {v_424}, new Results(v_425));
V v_448 = values_447[0];
V v_449 = values_447[1];
if (R.Truth((V)(R.Get(v_424, (V)"x") > R.Get(v_425, (V)"x")))) {
var values_450 = R.Join(new V[] {v_425}, new Results(v_424));
v_448 = values_450[0];
v_449 = values_450[1];
}
var values_451 = R.Join(new V[] {}, new Results((V)(R.Get(v_449, (V)"x") - R.Get(v_448, (V)"x"))));
V v_452 = values_451[0];
var values_453 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_452 / (V)2.0d)))).First + R.Or(R.And((V)(R.Get(v_427, (V)"strategy") == (V)"archive"), () => (V)0.0d), () => (V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_426}, new Results((V)3.0d))).First - (V)2.0d)))));
V v_454 = values_453[0];
var values_455 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)(R.Get(v_448, (V)"x") + (V)2.0d)}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)(R.Get(v_449, (V)"x") - (V)2.0d)}, new Results((V)(R.Get(v_448, (V)"x") + v_454)))))));
V v_456 = values_455[0];
R.Call(v_430, R.Join(new V[] {R.Get(v_448, (V)"x"), R.Get(v_448, (V)"y"), v_456}, new Results(R.Get(v_448, (V)"y"))));
R.Call(v_430, R.Join(new V[] {v_456, R.Get(v_448, (V)"y"), v_456}, new Results(R.Get(v_449, (V)"y"))));
R.Call(v_430, R.Join(new V[] {v_456, R.Get(v_449, (V)"y"), R.Get(v_449, (V)"x")}, new Results(R.Get(v_449, (V)"y"))));
} else {
var values_457 = R.Join(new V[] {v_424}, new Results(v_425));
V v_458 = values_457[0];
V v_459 = values_457[1];
if (R.Truth((V)(R.Get(v_424, (V)"y") > R.Get(v_425, (V)"y")))) {
var values_460 = R.Join(new V[] {v_425}, new Results(v_424));
v_458 = values_460[0];
v_459 = values_460[1];
}
var values_461 = R.Join(new V[] {}, new Results((V)(R.Get(v_459, (V)"y") - R.Get(v_458, (V)"y"))));
V v_462 = values_461[0];
var values_463 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)(v_462 / (V)2.0d)))).First + R.Or(R.And((V)(R.Get(v_427, (V)"strategy") == (V)"archive"), () => (V)0.0d), () => (V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_426}, new Results((V)3.0d))).First - (V)2.0d)))));
V v_464 = values_463[0];
var values_465 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {(V)(R.Get(v_458, (V)"y") + (V)2.0d)}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {(V)(R.Get(v_459, (V)"y") - (V)2.0d)}, new Results((V)(R.Get(v_458, (V)"y") + v_464)))))));
V v_466 = values_465[0];
R.Call(v_430, R.Join(new V[] {R.Get(v_458, (V)"x"), R.Get(v_458, (V)"y"), R.Get(v_458, (V)"x")}, new Results(v_466)));
R.Call(v_430, R.Join(new V[] {R.Get(v_458, (V)"x"), v_466, R.Get(v_459, (V)"x")}, new Results(v_466)));
R.Call(v_430, R.Join(new V[] {R.Get(v_459, (V)"x"), v_466, R.Get(v_459, (V)"x")}, new Results(R.Get(v_459, (V)"y"))));
}
if (R.Truth(R.And((V)(R.Get(v_427, (V)"strategy") == (V)"warrens"), () => (V)((V)R.Length(v_429) >= (V)12.0d)))) {
var values_467 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)R.Length(v_429) / (V)3.0d)))));
V v_468 = values_467[0];
var values_469 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(v_429) * (V)2.0d) / (V)3.0d)))));
V v_470 = values_469[0];
var values_471 = R.Join(new V[] {R.Get(v_429, v_468)}, new Results(R.Get(v_429, v_470)));
V v_472 = values_471[0];
V v_473 = values_471[1];
if (R.Truth(R.And(R.And(R.And(v_472, () => v_473), () => R.Or((V)(R.Get(v_472, (V)"x") == R.Get(v_473, (V)"x")), () => (V)(R.Get(v_472, (V)"y") == R.Get(v_473, (V)"y")))), () => (V)((V)(v_470 - v_468) >= (V)5.0d)))) {
var values_474 = R.Join(new V[] {}, new Results(R.Table()));
V v_475 = values_474[0];
{ double end_478 = R.Num(v_468), step_479 = R.Num((V)1.0d);
for (double i_477 = R.Num((V)1.0d); step_479 > 0 ? i_477 <= end_478 : i_477 >= end_478; i_477 += step_479) {
V v_476 = i_477;
var values_480 = R.Join(new V[] {}, new Results(R.Get(v_429, v_476)));
var obj_481 = v_475;
V key_482 = (V)((V)R.Length(v_475) + (V)1.0d);
R.Set(obj_481, key_482, values_480[0]);
}
}
var values_483 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_426}, new Results((V)2.0d))).First == (V)1.0d), () => (V)2.0d), () => (-(V)2.0d))));
V v_484 = values_483[0];
V v_485 = V.Nil;
v_485 = new V((RuleFunction)(args_486 => {
V v_487 = args_486[0];
V v_488 = args_486[1];
var values_489 = R.Join(new V[] {}, new Results(R.Get(v_475, (V)R.Length(v_475))));
V v_490 = values_489[0];
while (R.Truth(R.Or((V)(R.Get(v_490, (V)"x") != v_487), () => (V)(R.Get(v_490, (V)"y") != v_488)))) {
var values_491 = R.Join(new V[] {}, new Results((V)(R.Get(v_490, (V)"x") + R.Or(R.And((V)(R.Get(v_490, (V)"x") == v_487), () => (V)0.0d), () => R.Or(R.And((V)(v_487 > R.Get(v_490, (V)"x")), () => (V)1.0d), () => (-(V)1.0d))))));
V v_492 = values_491[0];
var values_493 = R.Join(new V[] {}, new Results((V)(R.Get(v_490, (V)"y") + R.Or(R.And((V)(R.Get(v_490, (V)"x") != v_487), () => (V)0.0d), () => R.Or(R.And((V)(v_488 > R.Get(v_490, (V)"y")), () => (V)1.0d), () => (-(V)1.0d))))));
V v_494 = values_493[0];
var values_495 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_492), new Entry((V)"y", v_494))));
var obj_496 = v_475;
V key_497 = (V)((V)R.Length(v_475) + (V)1.0d);
R.Set(obj_496, key_497, values_495[0]);

var values_498 = R.Join(new V[] {}, new Results(R.Get(v_475, (V)R.Length(v_475))));
v_490 = values_498[0];
}
return Results.Empty;
}));
if (R.Truth((V)(R.Get(v_472, (V)"y") == R.Get(v_473, (V)"y")))) {
R.Call(v_485, R.Join(new V[] {R.Get(v_472, (V)"x")}, new Results((V)(R.Get(v_472, (V)"y") + v_484))));

R.Call(v_485, R.Join(new V[] {R.Get(v_473, (V)"x")}, new Results((V)(R.Get(v_472, (V)"y") + v_484))));
} else {
R.Call(v_485, R.Join(new V[] {(V)(R.Get(v_472, (V)"x") + v_484)}, new Results(R.Get(v_472, (V)"y"))));

R.Call(v_485, R.Join(new V[] {(V)(R.Get(v_472, (V)"x") + v_484)}, new Results(R.Get(v_473, (V)"y"))));
}
R.Call(v_485, R.Join(new V[] {R.Get(v_473, (V)"x")}, new Results(R.Get(v_473, (V)"y"))));
{ double end_501 = R.Num((V)R.Length(v_429)), step_502 = R.Num((V)1.0d);
for (double i_500 = R.Num((V)(v_470 + (V)1.0d)); step_502 > 0 ? i_500 <= end_501 : i_500 >= end_501; i_500 += step_502) {
V v_499 = i_500;
var values_503 = R.Join(new V[] {}, new Results(R.Get(v_429, v_499)));
var obj_504 = v_475;
V key_505 = (V)((V)R.Length(v_475) + (V)1.0d);
R.Set(obj_504, key_505, values_503[0]);
}
}
var values_506 = R.Join(new V[] {}, new Results(v_475));
v_429 = values_506[0];
}
}
return R.Join(new V[] {}, new Results(v_429));
return Results.Empty;
}));
V v_507 = V.Nil;
v_507 = new V((RuleFunction)(args_508 => {
V v_509 = args_508[0];
V v_510 = args_508[1];
var values_511 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_509, (V)"layout"), () => R.Get(R.Get(v_509, (V)"layout"), (V)"chest_target")), () => R.Get(v_18, (V)"chest_target"))));
V v_512 = values_511[0];
var values_513 = R.Join(new V[] {}, new Results(R.Table()));
V v_514 = values_513[0];
var values_515 = R.Join(new V[] {}, new Results(R.Table()));
V v_516 = values_515[0];
foreach (var row_517 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_509, (V)"corridor_quests"), () => R.Table()))))))) {
V v_518 = row_517[0];
V v_519 = row_517[1];
foreach (var row_520 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_519, (V)"cells"))))))) {
V v_521 = row_520[0];
V v_522 = row_520[1];
var values_523 = R.Join(new V[] {}, new Results((V)true));
var obj_524 = v_516;
V key_525 = R.Call(v_65, R.Join(new V[] {R.Get(v_522, (V)"x")}, new Results(R.Get(v_522, (V)"y")))).First;
R.Set(obj_524, key_525, values_523[0]);
}
}
var values_526 = R.Join(new V[] {}, new Results(R.Table()));
V v_527 = values_526[0];
foreach (var row_528 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_509, (V)"rooms"))))))) {
V v_529 = row_528[0];
V v_530 = row_528[1];
if (R.Truth(R.And((V)(R.Get(v_530, (V)"role") != (V)"start"), () => (V)(R.Get(v_530, (V)"role") != (V)"elite")))) {
var values_531 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_530, (V)"markers"), (V)"chest"), (V)1.0d)));
V v_532 = values_531[0];
if (R.Truth(v_532)) {
var values_533 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"room", v_530), new Entry((V)"point", v_532))));
var obj_534 = v_527;
V key_535 = (V)((V)R.Length(v_527) + (V)1.0d);
R.Set(obj_534, key_535, values_533[0]);
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_527) >= v_512)}, new Results((V)"too few chest markers")));
{ double end_538 = R.Num(v_512), step_539 = R.Num((V)1.0d);
for (double i_537 = R.Num((V)1.0d); step_539 > 0 ? i_537 <= end_538 : i_537 >= end_538; i_537 += step_539) {
V v_536 = i_537;
var values_540 = R.Join(new V[] {}, new Results(R.Get(v_527, (V)(R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)(v_536 - (V)1.0d) * (V)((V)R.Length(v_527) - (V)1.0d)) / (V)(v_512 - (V)1.0d))))).First + (V)1.0d))));
V v_541 = values_540[0];
var values_542 = R.Join(new V[] {}, new Results(R.Get(v_541, (V)"point")));
V v_543 = values_542[0];
var values_544 = R.Join(new V[] {}, new Results(R.Concat((V)"chest:", R.Get(R.Get(v_541, (V)"room"), (V)"id"))));
V v_545 = values_544[0];
var values_546 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_545), new Entry((V)"definition_id", (V)"core:chest/standard"), new Entry((V)"x", R.Get(v_543, (V)"x")), new Entry((V)"y", R.Get(v_543, (V)"y")), new Entry((V)"room_id", R.Get(R.Get(v_541, (V)"room"), (V)"id")))));
var obj_547 = R.Get(R.Get(v_509, (V)"placements"), (V)"chests");
V key_548 = (V)((V)R.Length(R.Get(R.Get(v_509, (V)"placements"), (V)"chests")) + (V)1.0d);
R.Set(obj_547, key_548, values_546[0]);
var values_549 = R.Join(new V[] {}, new Results((V)true));
var obj_550 = v_514;
V key_551 = R.Call(v_65, R.Join(new V[] {R.Get(v_543, (V)"x")}, new Results(R.Get(v_543, (V)"y")))).First;
R.Set(obj_550, key_551, values_549[0]);
}
}
var values_552 = R.Join(new V[] {}, new Results(R.Get(v_510, (V)"enemy_ids")));
V v_553 = values_552[0];
var values_554 = R.Join(new V[] {}, new Results((V)0.0d));
V v_555 = values_554[0];
foreach (var row_556 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_509, (V)"rooms"))))))) {
V v_557 = row_556[0];
V v_558 = row_556[1];
if (R.Truth(R.And((V)(R.Get(v_558, (V)"role") != (V)"start"), () => (V)(R.Get(v_558, (V)"role") != (V)"elite")))) {
var values_559 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_558, (V)"markers"), (V)"enemy"), () => R.Table())));
V v_560 = values_559[0];
var values_561 = R.Join(new V[] {}, new Results(R.Or(R.And((V)((V)R.Length(v_560) > (V)0.0d), () => R.Get(v_560, (V)((V)((V)(R.Get(v_558, (V)"id") - (V)1.0d) % (V)R.Length(v_560)) + (V)1.0d))), () => V.Nil)));
V v_562 = values_561[0];
foreach (var row_563 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(v_562, () => R.Table(new Entry((V)1, v_562))), () => R.Table()))))))) {
V v_564 = row_563[0];
V v_565 = row_563[1];
if (R.Truth((V)(!R.Truth(R.Get(v_514, R.Call(v_65, R.Join(new V[] {R.Get(v_565, (V)"x")}, new Results(R.Get(v_565, (V)"y")))).First))))) {
var values_566 = R.Join(new V[] {}, new Results((V)(v_555 + (V)1.0d)));
v_555 = values_566[0];
var values_567 = R.Join(new V[] {}, new Results(R.Concat((V)"enemy:", R.Concat(R.Get(v_558, (V)"id"), R.Concat((V)":", v_555)))));
V v_568 = values_567[0];
var values_569 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", v_568), new Entry((V)"definition_id", R.Get(v_553, (V)((V)((V)(v_555 - (V)1.0d) % (V)3.0d) + (V)1.0d))), new Entry((V)"x", R.Get(v_565, (V)"x")), new Entry((V)"y", R.Get(v_565, (V)"y")), new Entry((V)"room_id", R.Get(v_558, (V)"id")))));
var obj_570 = R.Get(R.Get(v_509, (V)"placements"), (V)"enemies");
V key_571 = (V)((V)R.Length(R.Get(R.Get(v_509, (V)"placements"), (V)"enemies")) + (V)1.0d);
R.Set(obj_570, key_571, values_569[0]);
var values_572 = R.Join(new V[] {}, new Results((V)true));
var obj_573 = v_514;
V key_574 = R.Call(v_65, R.Join(new V[] {R.Get(v_565, (V)"x")}, new Results(R.Get(v_565, (V)"y")))).First;
R.Set(obj_573, key_574, values_572[0]);
}
}
}
}
var values_575 = R.Join(new V[] {}, new Results((V)0.0d));
V v_576 = values_575[0];
foreach (var row_577 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_509, (V)"corridors"))))))) {
V v_578 = row_577[0];
V v_579 = row_577[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_579, (V)"length") >= (V)24.0d), () => (V)((V)R.Length(R.Get(v_579, (V)"alcoves")) > (V)0.0d)), () => (V)(v_576 < (V)2.0d)))) {
var values_580 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_579, (V)"alcoves"), R.Call(R.Get(rt.Global("math"), (V)"floor"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(v_579, (V)"alcoves")) + (V)1.0d) / (V)2.0d)))).First)));
V v_581 = values_580[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_514, R.Call(v_65, R.Join(new V[] {R.Get(v_581, (V)"x")}, new Results(R.Get(v_581, (V)"y")))).First))), () => (V)(!R.Truth(R.Get(v_516, R.Call(v_65, R.Join(new V[] {R.Get(v_581, (V)"x")}, new Results(R.Get(v_581, (V)"y")))).First)))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_581, (V)"x") - R.Get(v_509, (V)"start_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_581, (V)"y") - R.Get(v_509, (V)"start_y"))))).First) >= (V)8.0d)))) {
var values_582 = R.Join(new V[] {}, new Results((V)(v_555 + (V)1.0d)));
v_555 = values_582[0];
var values_583 = R.Join(new V[] {}, new Results((V)(v_576 + (V)1.0d)));
v_576 = values_583[0];
var values_584 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"corridor_enemy:", R.Get(v_579, (V)"edge_id"))), new Entry((V)"definition_id", R.Get(v_553, (V)((V)((V)(v_555 - (V)1.0d) % (V)3.0d) + (V)1.0d))), new Entry((V)"x", R.Get(v_581, (V)"x")), new Entry((V)"y", R.Get(v_581, (V)"y")), new Entry((V)"corridor_id", R.Get(v_579, (V)"edge_id")))));
var obj_585 = R.Get(R.Get(v_509, (V)"placements"), (V)"enemies");
V key_586 = (V)((V)R.Length(R.Get(R.Get(v_509, (V)"placements"), (V)"enemies")) + (V)1.0d);
R.Set(obj_585, key_586, values_584[0]);
var values_587 = R.Join(new V[] {}, new Results((V)true));
var obj_588 = v_514;
V key_589 = R.Call(v_65, R.Join(new V[] {R.Get(v_581, (V)"x")}, new Results(R.Get(v_581, (V)"y")))).First;
R.Set(obj_588, key_589, values_587[0]);
}
}
}
foreach (var row_590 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_509, (V)"corridor_quests"), () => R.Table()))))))) {
V v_591 = row_590[0];
V v_592 = row_590[1];
if (R.Truth((V)(R.Get(v_592, (V)"kind") == (V)"false_end"))) {
foreach (var row_593 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_592, (V)"nodes"))))))) {
V v_594 = row_593[0];
V v_595 = row_593[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_595, (V)"kind") == (V)"enemy"), () => (V)(!R.Truth(R.Get(v_514, R.Call(v_65, R.Join(new V[] {R.Get(v_595, (V)"x")}, new Results(R.Get(v_595, (V)"y")))).First)))), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_595, (V)"x") - R.Get(v_509, (V)"start_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_595, (V)"y") - R.Get(v_509, (V)"start_y"))))).First) >= (V)5.0d)))) {
var values_596 = R.Join(new V[] {}, new Results((V)(v_555 + (V)1.0d)));
v_555 = values_596[0];
var values_597 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"quest_enemy:", R.Get(v_592, (V)"id"))), new Entry((V)"definition_id", R.Get(v_553, (V)((V)((V)(v_555 - (V)1.0d) % (V)3.0d) + (V)1.0d))), new Entry((V)"x", R.Get(v_595, (V)"x")), new Entry((V)"y", R.Get(v_595, (V)"y")), new Entry((V)"corridor_id", R.Get(v_592, (V)"edge_id")))));
var obj_598 = R.Get(R.Get(v_509, (V)"placements"), (V)"enemies");
V key_599 = (V)((V)R.Length(R.Get(R.Get(v_509, (V)"placements"), (V)"enemies")) + (V)1.0d);
R.Set(obj_598, key_599, values_597[0]);
var values_600 = R.Join(new V[] {}, new Results((V)true));
var obj_601 = v_514;
V key_602 = R.Call(v_65, R.Join(new V[] {R.Get(v_595, (V)"x")}, new Results(R.Get(v_595, (V)"y")))).First;
R.Set(obj_601, key_602, values_600[0]);
}
}
}
}
return Results.Empty;
}));
V v_603 = V.Nil;
v_603 = new V((RuleFunction)(args_604 => {
V v_605 = args_604[0];
V v_606 = args_604[1];
V v_607 = args_604[2];
V v_608 = args_604[3];
V v_609 = args_604[4];
V v_610 = args_604[5];
var values_611 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {v_609}, new Results(R.Concat(R.Get(v_605, (V)"floor_id"), R.Concat((V)":secrets:", v_610))))))));
V v_612 = values_611[0];
var values_613 = R.Join(new V[] {}, new Results(R.Table()));
V v_614 = values_613[0];
foreach (var row_615 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_605, (V)"graph"), (V)"critical"))))))) {
V v_616 = row_615[0];
V v_617 = row_615[1];
var values_618 = R.Join(new V[] {}, new Results((V)true));
var obj_619 = v_614;
V key_620 = v_617;
R.Set(obj_619, key_620, values_618[0]);
}
var values_621 = R.Join(new V[] {}, new Results(R.Table()));
V v_622 = values_621[0];
foreach (var row_623 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_605, (V)"rooms"))))))) {
V v_624 = row_623[0];
V v_625 = row_623[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(!R.Truth(R.Get(v_614, R.Get(v_625, (V)"id")))), () => R.Get(R.Get(v_625, (V)"markers"), (V)"secret_anchor")), () => (V)((V)R.Length(R.Get(R.Get(v_625, (V)"markers"), (V)"secret_anchor")) > (V)0.0d)), () => R.Get(R.Get(v_625, (V)"markers"), (V)"secret_wall")), () => (V)((V)R.Length(R.Get(R.Get(v_625, (V)"markers"), (V)"secret_wall")) > (V)0.0d)))) {
var values_626 = R.Join(new V[] {}, new Results(v_625));
var obj_627 = v_622;
V key_628 = (V)((V)R.Length(v_622) + (V)1.0d);
R.Set(obj_627, key_628, values_626[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_622) >= (V)2.0d)}, new Results((V)"secret placement needs two optional rooms")));
var values_629 = R.Join(new V[] {}, new Results(R.Table()));
V v_630 = values_629[0];
foreach (var row_631 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_622)))))) {
V v_632 = row_631[0];
V v_633 = row_631[1];
var values_634 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_633, (V)"markers"), (V)"secret_wall"), (V)1.0d)));
V v_635 = values_634[0];
var values_636 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_635, (V)"x") == R.Get(v_633, (V)"x")), () => (-(V)1.0d)), () => R.Or(R.And((V)(R.Get(v_635, (V)"x") == (V)((V)(R.Get(v_633, (V)"x") + R.Get(v_633, (V)"width")) - (V)1.0d)), () => (V)1.0d), () => (V)0.0d))));
V v_637 = values_636[0];
var values_638 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_635, (V)"y") == R.Get(v_633, (V)"y")), () => (-(V)1.0d)), () => R.Or(R.And((V)(R.Get(v_635, (V)"y") == (V)((V)(R.Get(v_633, (V)"y") + R.Get(v_633, (V)"height")) - (V)1.0d)), () => (V)1.0d), () => (V)0.0d))));
V v_639 = values_638[0];
var values_640 = R.Join(new V[] {(V)(R.Get(v_635, (V)"x") + v_637)}, new Results((V)(R.Get(v_635, (V)"y") + v_639)));
V v_641 = values_640[0];
V v_642 = values_640[1];
if (R.Truth(R.And(R.And(R.And(R.And(R.And(R.And((V)(v_641 > (V)1.0d), () => (V)(v_641 < R.Get(v_605, (V)"width"))), () => (V)(v_642 > (V)1.0d)), () => (V)(v_642 < R.Get(v_605, (V)"height"))), () => (V)(R.Get(R.Get(v_606, R.Get(v_635, (V)"y")), R.Get(v_635, (V)"x")) == (V)"#")), () => (V)(R.Get(R.Get(v_606, v_642), v_641) == (V)"#")), () => (V)(!R.Truth(R.Get(v_608, R.Call(v_65, R.Join(new V[] {v_641}, new Results(v_642))).First)))))) {
var values_643 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"room", v_633), new Entry((V)"wall", v_635), new Entry((V)"cache_x", v_641), new Entry((V)"cache_y", v_642))));
var obj_644 = v_630;
V key_645 = (V)((V)R.Length(v_630) + (V)1.0d);
R.Set(obj_644, key_645, values_643[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_630) > (V)0.0d)}, new Results((V)"no protected attack-secret alcove")));
var values_646 = R.Join(new V[] {}, new Results(R.Get(v_630, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_612}, new Results((V)R.Length(v_630)))).First)));
V v_647 = values_646[0];
var values_648 = R.Join(new V[] {}, new Results(R.Table()));
V v_649 = values_648[0];
foreach (var row_650 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_622)))))) {
V v_651 = row_650[0];
V v_652 = row_650[1];
if (R.Truth((V)(R.Get(v_652, (V)"id") != R.Get(R.Get(v_647, (V)"room"), (V)"id")))) {
var values_653 = R.Join(new V[] {}, new Results(v_652));
var obj_654 = v_649;
V key_655 = (V)((V)R.Length(v_649) + (V)1.0d);
R.Set(obj_654, key_655, values_653[0]);
}
}
var values_656 = R.Join(new V[] {}, new Results(R.Get(v_649, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_612}, new Results((V)R.Length(v_649)))).First)));
V v_657 = values_656[0];
var values_658 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_647, (V)"room"), (V)"markers"), (V)"secret_anchor"), (V)1.0d)));
V v_659 = values_658[0];
var values_660 = R.Join(new V[] {}, new Results(R.Or(R.And(R.Get(v_605, (V)"layout"), () => R.Get(R.Get(v_605, (V)"layout"), (V)"secret_kind")), () => (V)"both")));
V v_661 = values_660[0];
if (R.Truth(R.Or((V)(v_661 == (V)"both"), () => (V)(v_661 == (V)"cracked")))) {
var values_662 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"secret:cracked:", R.Get(R.Get(v_647, (V)"room"), (V)"id"))), new Entry((V)"definition_id", (V)"core:secret/cracked_cache"), new Entry((V)"room_id", R.Get(R.Get(v_647, (V)"room"), (V)"id")), new Entry((V)"x", R.Get(v_659, (V)"x")), new Entry((V)"y", R.Get(v_659, (V)"y")), new Entry((V)"wall_x", R.Get(R.Get(v_647, (V)"wall"), (V)"x")), new Entry((V)"wall_y", R.Get(R.Get(v_647, (V)"wall"), (V)"y")), new Entry((V)"cache_x", R.Get(v_647, (V)"cache_x")), new Entry((V)"cache_y", R.Get(v_647, (V)"cache_y")))));
var obj_663 = R.Get(R.Get(v_605, (V)"placements"), (V)"secrets");
V key_664 = (V)((V)R.Length(R.Get(R.Get(v_605, (V)"placements"), (V)"secrets")) + (V)1.0d);
R.Set(obj_663, key_664, values_662[0]);
var values_665 = R.Join(new V[] {}, new Results((V)"k"));
var obj_666 = R.Get(v_607, R.Get(R.Get(v_647, (V)"wall"), (V)"y"));
V key_667 = R.Get(R.Get(v_647, (V)"wall"), (V)"x");
R.Set(obj_666, key_667, values_665[0]);
}
var values_668 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_657, (V)"markers"), (V)"secret_anchor"), (V)1.0d)));
V v_669 = values_668[0];
if (R.Truth(R.Or((V)(v_661 == (V)"both"), () => (V)(v_661 == (V)"sigil")))) {
var values_670 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"secret:sigil:", R.Get(v_657, (V)"id"))), new Entry((V)"definition_id", (V)"core:secret/moss_sigil"), new Entry((V)"room_id", R.Get(v_657, (V)"id")), new Entry((V)"x", R.Get(v_669, (V)"x")), new Entry((V)"y", R.Get(v_669, (V)"y")))));
var obj_671 = R.Get(R.Get(v_605, (V)"placements"), (V)"secrets");
V key_672 = (V)((V)R.Length(R.Get(R.Get(v_605, (V)"placements"), (V)"secrets")) + (V)1.0d);
R.Set(obj_671, key_672, values_670[0]);
var values_673 = R.Join(new V[] {}, new Results((V)"p"));
var obj_674 = R.Get(v_607, R.Get(v_669, (V)"y"));
V key_675 = R.Get(v_669, (V)"x");
R.Set(obj_674, key_675, values_673[0]);
}
var values_676 = R.Join(new V[] {}, new Results(R.Table()));
V v_677 = values_676[0];
foreach (var row_678 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_605, (V)"graph"), (V)"edges"))))))) {
V v_679 = row_678[0];
V v_680 = row_678[1];
var values_681 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_677, R.Get(v_680, (V)"a")), () => (V)0.0d) + (V)1.0d)));
var obj_682 = v_677;
V key_683 = R.Get(v_680, (V)"a");
R.Set(obj_682, key_683, values_681[0]);
var values_684 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_677, R.Get(v_680, (V)"b")), () => (V)0.0d) + (V)1.0d)));
var obj_685 = v_677;
V key_686 = R.Get(v_680, (V)"b");
R.Set(obj_685, key_686, values_684[0]);
}
var values_687 = Results.Empty;
V v_688 = values_687[0];
foreach (var row_689 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_622)))))) {
V v_690 = row_689[0];
V v_691 = row_689[1];
if (R.Truth(R.And((V)(R.Get(v_677, R.Get(v_691, (V)"id")) == (V)1.0d), () => (V)(R.Get(v_691, (V)"id") != R.Get(R.Get(v_647, (V)"room"), (V)"id"))))) {
var values_692 = R.Join(new V[] {}, new Results(v_691));
v_688 = values_692[0];

break;
}
}
var values_693 = R.Join(new V[] {}, new Results(R.Or(v_688, () => v_657)));
v_688 = values_693[0];
var values_694 = Results.Empty;
V v_695 = values_694[0];
foreach (var row_696 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_605, (V)"corridors"))))))) {
V v_697 = row_696[0];
V v_698 = row_696[1];
if (R.Truth((V)(R.Get(v_698, (V)"from_room") == R.Get(v_688, (V)"id")))) {
var values_699 = R.Join(new V[] {}, new Results(R.Get(v_698, (V)"from_socket")));
v_695 = values_699[0];

break;
}
if (R.Truth((V)(R.Get(v_698, (V)"to_room") == R.Get(v_688, (V)"id")))) {
var values_700 = R.Join(new V[] {}, new Results(R.Get(v_698, (V)"to_socket")));
v_695 = values_700[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_695}, new Results((V)"optional room lacks door connection")));
var values_701 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Concat((V)"door:", R.Get(v_688, (V)"id"))), new Entry((V)"definition_id", (V)"core:door/ruin"), new Entry((V)"room_id", R.Get(v_688, (V)"id")), new Entry((V)"x", R.Get(v_695, (V)"x")), new Entry((V)"y", R.Get(v_695, (V)"y")))));
var obj_702 = R.Get(R.Get(v_605, (V)"placements"), (V)"doors");
V key_703 = (V)1.0d;
R.Set(obj_702, key_703, values_701[0]);
return Results.Empty;
}));
V v_704 = V.Nil;
v_704 = new V((RuleFunction)(args_705 => {
V v_706 = args_705[0];
V v_707 = args_705[1];
if (R.Truth((V)(!R.Truth(R.Get(v_706, (V)"layout"))))) {
return Results.Empty;
}
var values_708 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {v_707}, new Results(R.Concat(R.Get(v_706, (V)"floor_id"), (V)":terrain")))))));
V v_709 = values_708[0];
var values_710 = R.Join(new V[] {}, new Results(R.Table()));
V v_711 = values_710[0];
V v_712 = V.Nil;
v_712 = new V((RuleFunction)(args_713 => {
V v_714 = args_713[0];
V v_715 = args_713[1];
V v_716 = args_713[2];
var values_717 = R.Join(new V[] {}, new Results(R.Or(v_716, () => (V)0.0d)));
v_716 = values_717[0];
{ double end_720 = R.Num(v_716), step_721 = R.Num((V)1.0d);
for (double i_719 = R.Num((-v_716)); step_721 > 0 ? i_719 <= end_720 : i_719 >= end_720; i_719 += step_721) {
V v_718 = i_719;
{ double end_724 = R.Num(v_716), step_725 = R.Num((V)1.0d);
for (double i_723 = R.Num((-v_716)); step_725 > 0 ? i_723 <= end_724 : i_723 >= end_724; i_723 += step_725) {
V v_722 = i_723;
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_722))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results(v_718))).First) <= v_716))) {
var values_726 = R.Join(new V[] {}, new Results((V)true));
var obj_727 = v_711;
V key_728 = R.Call(v_65, R.Join(new V[] {(V)(v_714 + v_722)}, new Results((V)(v_715 + v_718)))).First;
R.Set(obj_727, key_728, values_726[0]);
}
}
}
}
}
return Results.Empty;
}));
R.Call(v_712, R.Join(new V[] {R.Get(v_706, (V)"start_x"), R.Get(v_706, (V)"start_y")}, new Results((V)4.0d)));
R.Call(v_712, R.Join(new V[] {R.Get(R.Get(v_706, (V)"elite"), (V)"x"), R.Get(R.Get(v_706, (V)"elite"), (V)"y")}, new Results((V)4.0d)));
foreach (var row_729 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"enemies"), new Entry((V)2, (V)"chests"), new Entry((V)3, (V)"doors")))))))) {
V v_730 = row_729[0];
V v_731 = row_729[1];
foreach (var row_732 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_706, (V)"placements"), v_731), () => R.Table()))))))) {
V v_733 = row_732[0];
V v_734 = row_732[1];
R.Call(v_712, R.Join(new V[] {R.Get(v_734, (V)"x"), R.Get(v_734, (V)"y")}, new Results((V)1.0d)));
}
}
foreach (var row_735 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_706, (V)"placements"), (V)"secrets"), () => R.Table()))))))) {
V v_736 = row_735[0];
V v_737 = row_735[1];
R.Call(v_712, R.Join(new V[] {R.Get(v_737, (V)"x"), R.Get(v_737, (V)"y")}, new Results((V)1.0d)));
if (R.Truth(R.Get(v_737, (V)"wall_x"))) {
R.Call(v_712, R.Join(new V[] {R.Get(v_737, (V)"wall_x"), R.Get(v_737, (V)"wall_y")}, new Results((V)1.0d)));
}
}
var values_738 = R.Join(new V[] {}, new Results(R.Table()));
V v_739 = values_738[0];
foreach (var row_740 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_706, (V)"corridors"))))))) {
V v_741 = row_740[0];
V v_742 = row_740[1];
foreach (var row_743 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_742, (V)"cells"))))))) {
V v_744 = row_743[0];
V v_745 = row_743[1];
var values_746 = R.Join(new V[] {}, new Results((V)true));
var obj_747 = v_739;
V key_748 = R.Call(v_65, R.Join(new V[] {R.Get(v_745, (V)"x")}, new Results(R.Get(v_745, (V)"y")))).First;
R.Set(obj_747, key_748, values_746[0]);
}
}
var values_749 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_706, (V)"floor_index") == (V)1.0d), () => R.Table(new Entry((V)1, (V)"b"), new Entry((V)2, (V)"g"), new Entry((V)3, (V)"i"))), () => R.Table(new Entry((V)1, (V)"B"), new Entry((V)2, (V)"m"), new Entry((V)3, (V)"c")))));
V v_750 = values_749[0];
foreach (var row_751 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_750)))))) {
V v_752 = row_751[0];
V v_753 = row_751[1];
var values_754 = R.Join(new V[] {}, new Results(R.Table()));
V v_755 = values_754[0];
var values_756 = R.Join(new V[] {}, new Results(R.Or(R.Or((V)(v_753 == (V)"b"), () => (V)(v_753 == (V)"B")), () => (V)(v_753 == (V)"c"))));
V v_757 = values_756[0];
foreach (var row_758 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_706, (V)"editable_cells"))))))) {
V v_759 = row_758[0];
V v_760 = row_758[1];
var values_761 = R.Join(new V[] {}, new Results(R.Or(R.And(v_757, () => R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_760, (V)"kind") == (V)"alcove"), () => (V)(R.Get(v_760, (V)"kind") == (V)"parallel_bank")), () => (V)(R.Get(v_760, (V)"kind") == (V)"courtyard_node")), () => (V)(R.Get(v_760, (V)"kind") == (V)"star_node")), () => (V)(R.Get(v_760, (V)"kind") == (V)"forge_bay"))), () => R.And((V)(!R.Truth(v_757)), () => (V)(R.Get(v_760, (V)"kind") == (V)"corridor")))));
V v_762 = values_761[0];
if (R.Truth(R.And(R.And(R.And(v_762, () => (V)(!R.Truth(R.Get(v_711, R.Call(v_65, R.Join(new V[] {R.Get(v_760, (V)"x")}, new Results(R.Get(v_760, (V)"y")))).First)))), () => R.Or((V)(v_753 != (V)"B"), () => (V)(!R.Truth(R.Get(v_739, R.Call(v_65, R.Join(new V[] {R.Get(v_760, (V)"x")}, new Results(R.Get(v_760, (V)"y")))).First))))), () => (V)(R.Call(R.Get(v_14, (V)"symbol"), R.Join(new V[] {v_706, R.Get(v_760, (V)"x")}, new Results(R.Get(v_760, (V)"y")))).First == (V)".")))) {
var values_763 = R.Join(new V[] {}, new Results(v_760));
var obj_764 = v_755;
V key_765 = (V)((V)R.Length(v_755) + (V)1.0d);
R.Set(obj_764, key_765, values_763[0]);
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {(V)((V)R.Length(v_755) > (V)0.0d)}, new Results(R.Concat((V)"no safe terrain candidate for ", v_753))));
var values_766 = R.Join(new V[] {}, new Results(R.Get(v_755, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_709}, new Results((V)R.Length(v_755)))).First)));
V v_767 = values_766[0];
R.Call(v_277, R.Join(new V[] {v_706, R.Get(v_767, (V)"x"), R.Get(v_767, (V)"y")}, new Results(v_753)));
var values_768 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"symbol", v_753), new Entry((V)"id", R.Get(R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_753))).First, (V)"id")), new Entry((V)"x", R.Get(v_767, (V)"x")), new Entry((V)"y", R.Get(v_767, (V)"y")))));
var obj_769 = R.Get(v_706, (V)"terrain_placements");
V key_770 = (V)((V)R.Length(R.Get(v_706, (V)"terrain_placements")) + (V)1.0d);
R.Set(obj_769, key_770, values_768[0]);
R.Call(v_712, R.Join(new V[] {R.Get(v_767, (V)"x"), R.Get(v_767, (V)"y")}, new Results((V)2.0d)));
if (R.Truth((V)(v_753 == (V)"B"))) {
var values_771 = R.Join(new V[] {}, new Results((V)true));
var obj_772 = R.Get(v_706, (V)"breakable_origins");
V key_773 = R.Call(v_65, R.Join(new V[] {R.Get(v_767, (V)"x")}, new Results(R.Get(v_767, (V)"y")))).First;
R.Set(obj_772, key_773, values_771[0]);
}
}
return Results.Empty;
}));
V v_774 = V.Nil;
v_774 = new V((RuleFunction)(args_775 => {
V v_776 = args_775[0];
V v_777 = args_775[1];
V v_778 = args_775[2];
V v_779 = args_775[3];
V v_780 = args_775[4];
V v_781 = args_775[5];
var values_782 = R.Join(new V[] {}, new Results(R.Or(v_781, () => R.Table())));
v_781 = values_782[0];
var values_783 = R.Join(new V[] {}, new Results(R.Get(v_781, (V)"layout")));
V v_784 = values_783[0];
var values_785 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results(v_778))));
V v_786 = values_785[0];
var values_787 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"plan"), R.Join(new V[] {v_786, v_779}, new Results(v_784))));
V v_788 = values_787[0];
var values_789 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_779, (V)"map_id")), new Entry((V)"floor_id", R.Or(R.Get(v_781, (V)"floor_id"), () => R.Get(v_779, (V)"floor_id"))), new Entry((V)"biome_id", R.Get(v_779, (V)"id")), new Entry((V)"strategy", R.Get(v_779, (V)"strategy")), new Entry((V)"width", R.Or(R.And(v_784, () => R.Get(v_784, (V)"width")), () => R.Get(v_18, (V)"width"))), new Entry((V)"height", R.Or(R.And(v_784, () => R.Get(v_784, (V)"height")), () => R.Get(v_18, (V)"height"))), new Entry((V)"seed", v_776), new Entry((V)"attempt", v_777), new Entry((V)"derived_seed", v_778), new Entry((V)"layout", v_784), new Entry((V)"floor_index", R.Get(v_781, (V)"floor_index")), new Entry((V)"graph", v_788), new Entry((V)"rooms", R.Table()), new Entry((V)"corridors", R.Table()), new Entry((V)"protected_cells", R.Table()), new Entry((V)"editable_cells", R.Table()), new Entry((V)"placements", R.Table(new Entry((V)"enemies", R.Table()), new Entry((V)"chests", R.Table()), new Entry((V)"secrets", R.Table()), new Entry((V)"doors", R.Table()))), new Entry((V)"repairs", R.Table()), new Entry((V)"terrain_placements", R.Table()), new Entry((V)"breakable_origins", R.Table()), new Entry((V)"broken_barricades", R.Table()))));
V v_790 = values_789[0];
if (R.Truth(v_780)) {
var values_791 = R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {}, new Results(v_780))));
var obj_792 = v_790;
V key_793 = (V)"studio_pack_fingerprint";
R.Set(obj_792, key_793, values_791[0]);
var values_794 = R.Join(new V[] {}, new Results(R.Table()));
var obj_795 = v_790;
V key_796 = (V)"studio_templates";
R.Set(obj_795, key_796, values_794[0]);
}
var values_797 = R.Join(new V[] {}, R.Call(v_244, R.Join(new V[] {R.Get(v_790, (V)"width"), R.Get(v_790, (V)"height")}, new Results((V)"#"))));
V v_798 = values_797[0];
var values_799 = R.Join(new V[] {}, R.Call(v_244, R.Join(new V[] {R.Get(v_790, (V)"width"), R.Get(v_790, (V)"height")}, new Results((V)"w"))));
V v_800 = values_799[0];
var values_801 = R.Join(new V[] {}, new Results(R.Table()));
V v_802 = values_801[0];
var values_803 = R.Join(new V[] {}, new Results(R.Table()));
V v_804 = values_803[0];
foreach (var row_805 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_788, (V)"nodes"))))))) {
V v_806 = row_805[0];
V v_807 = row_805[1];
var values_808 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"for_role"), R.Join(new V[] {R.Get(v_807, (V)"role")}, new Results(R.Get(v_779, (V)"id")))));
V v_809 = values_808[0];
if (R.Truth(v_780)) {
foreach (var row_810 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_780, (V)"rooms"))))))) {
V v_811 = row_810[0];
V v_812 = row_810[1];
var values_813 = R.Join(new V[] {}, new Results((V)false));
V v_814 = values_813[0];
foreach (var row_815 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_812, (V)"biomes"))))))) {
V v_816 = row_815[0];
V v_817 = row_815[1];
if (R.Truth((V)(v_817 == R.Get(v_779, (V)"id")))) {
var values_818 = R.Join(new V[] {}, new Results((V)true));
v_814 = values_818[0];

break;
}
}
if (R.Truth(R.And(v_814, () => R.Call(R.Get(v_6, (V)"has_role"), R.Join(new V[] {v_812}, new Results(R.Get(v_807, (V)"role")))).First))) {
var values_819 = R.Join(new V[] {}, R.Call(R.Get(v_12, (V)"runtime_room"), R.Join(new V[] {v_812}, new Results(v_780))));
V v_820 = values_819[0];
{ double end_823 = R.Num(R.Get(v_812, (V)"weight")), step_824 = R.Num((V)1.0d);
for (double i_822 = R.Num((V)1.0d); step_824 > 0 ? i_822 <= end_823 : i_822 >= end_823; i_822 += step_824) {
V v_821 = i_822;
var values_825 = R.Join(new V[] {}, new Results(v_820));
var obj_826 = v_809;
V key_827 = (V)((V)R.Length(v_809) + (V)1.0d);
R.Set(obj_826, key_827, values_825[0]);
}
}
}
}
}
var values_828 = R.Join(new V[] {}, new Results(R.Or(R.And(R.And(R.And(R.Get(v_779, (V)"hub_template_id"), () => (V)(R.Get(v_807, (V)"id") == (V)6.0d)), () => (V)(!R.Truth(v_784))), () => R.Get(R.Get(v_6, (V)"definitions"), R.Get(v_779, (V)"hub_template_id"))), () => R.Get(v_809, R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_786}, new Results((V)R.Length(v_809)))).First))));
V v_829 = values_828[0];
if (R.Truth(R.And(v_780, () => R.Get(v_829, (V)"studio_weight")))) {
var values_830 = R.Join(new V[] {}, new Results(v_829));
var obj_831 = R.Get(v_790, (V)"studio_templates");
V key_832 = R.Get(v_829, (V)"id");
R.Set(obj_831, key_832, values_830[0]);
}
var values_833 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_829, (V)"rotations"), R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_786}, new Results((V)R.Length(R.Get(v_829, (V)"rotations"))))).First)));
V v_834 = values_833[0];
var values_835 = R.Join(new V[] {}, R.Call(R.Get(v_6, (V)"transform"), R.Join(new V[] {v_829, v_834}, new Results(R.And(R.Get(v_829, (V)"mirror"), () => (V)(R.Call(R.Get(v_4, (V)"next_int"), R.Join(new V[] {v_786}, new Results((V)2.0d))).First == (V)1.0d))))));
V v_836 = values_835[0];
var values_837 = R.Join(new V[] {}, R.Call(v_317, R.Join(new V[] {v_790, v_798, v_800, v_802, v_786, v_807, v_836}, new Results(v_779))));
V v_838 = values_837[0];
var values_839 = R.Join(new V[] {}, new Results(v_838));
var obj_840 = R.Get(v_790, (V)"rooms");
V key_841 = (V)((V)R.Length(R.Get(v_790, (V)"rooms")) + (V)1.0d);
R.Set(obj_840, key_841, values_839[0]);
var values_842 = R.Join(new V[] {}, new Results(v_838));
var obj_843 = v_804;
V key_844 = R.Get(v_807, (V)"id");
R.Set(obj_843, key_844, values_842[0]);
}
foreach (var row_845 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_788, (V)"edges"))))))) {
V v_846 = row_845[0];
V v_847 = row_845[1];
var values_848 = R.Join(new V[] {R.Get(v_804, R.Get(v_847, (V)"a"))}, new Results(R.Get(v_804, R.Get(v_847, (V)"b"))));
V v_849 = values_848[0];
V v_850 = values_848[1];
var values_851 = Results.Empty;
V v_852 = values_851[0];
V v_853 = values_851[1];
if (R.Truth(R.And(R.And((V)(R.Get(R.Get(v_849, (V)"center"), (V)"x") < R.Get(R.Get(v_850, (V)"center"), (V)"x")), () => (V)((V)(R.Get(R.Get(v_849, (V)"center"), (V)"y") - R.Get(R.Get(v_850, (V)"center"), (V)"y")) < (V)20.0d)), () => (V)((V)(R.Get(R.Get(v_849, (V)"center"), (V)"y") - R.Get(R.Get(v_850, (V)"center"), (V)"y")) > (-(V)20.0d))))) {
var values_854 = R.Join(new V[] {(V)"E"}, new Results((V)"W"));
v_852 = values_854[0];
v_853 = values_854[1];
} else if (R.Truth(R.And(R.And((V)(R.Get(R.Get(v_849, (V)"center"), (V)"x") > R.Get(R.Get(v_850, (V)"center"), (V)"x")), () => (V)((V)(R.Get(R.Get(v_849, (V)"center"), (V)"y") - R.Get(R.Get(v_850, (V)"center"), (V)"y")) < (V)20.0d)), () => (V)((V)(R.Get(R.Get(v_849, (V)"center"), (V)"y") - R.Get(R.Get(v_850, (V)"center"), (V)"y")) > (-(V)20.0d))))) {
var values_855 = R.Join(new V[] {(V)"W"}, new Results((V)"E"));
v_852 = values_855[0];
v_853 = values_855[1];
} else if (R.Truth((V)(R.Get(R.Get(v_849, (V)"center"), (V)"y") < R.Get(R.Get(v_850, (V)"center"), (V)"y")))) {
var values_856 = R.Join(new V[] {(V)"S"}, new Results((V)"N"));
v_852 = values_856[0];
v_853 = values_856[1];
} else {
var values_857 = R.Join(new V[] {(V)"N"}, new Results((V)"S"));
v_852 = values_857[0];
v_853 = values_857[1];
}
var values_858 = R.Join(new V[] {R.Call(v_310, R.Join(new V[] {v_849}, new Results(v_852))).First}, R.Call(v_310, R.Join(new V[] {v_850}, new Results(v_853))));
V v_859 = values_858[0];
V v_860 = values_858[1];
var values_861 = R.Join(new V[] {}, R.Call(v_422, R.Join(new V[] {v_859, v_860, v_786}, new Results(v_779))));
V v_862 = values_861[0];
var values_863 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"edge_id", R.Get(v_847, (V)"id")), new Entry((V)"from_room", R.Get(v_849, (V)"id")), new Entry((V)"to_room", R.Get(v_850, (V)"id")), new Entry((V)"from_socket", R.Table(new Entry((V)"x", R.Get(v_859, (V)"x")), new Entry((V)"y", R.Get(v_859, (V)"y")), new Entry((V)"facing", R.Get(v_859, (V)"facing")))), new Entry((V)"to_socket", R.Table(new Entry((V)"x", R.Get(v_860, (V)"x")), new Entry((V)"y", R.Get(v_860, (V)"y")), new Entry((V)"facing", R.Get(v_860, (V)"facing")))), new Entry((V)"cells", v_862), new Entry((V)"alcoves", R.Table()), new Entry((V)"length", (V)((V)R.Length(v_862) - (V)1.0d)), new Entry((V)"form", R.Or(R.And(R.Or((V)(R.Get(v_859, (V)"x") == R.Get(v_860, (V)"x")), () => (V)(R.Get(v_859, (V)"y") == R.Get(v_860, (V)"y"))), () => (V)"straight"), () => (V)"dogleg")))));
V v_864 = values_863[0];
foreach (var row_865 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_862)))))) {
V v_866 = row_865[0];
V v_867 = row_865[1];
var values_868 = R.Join(new V[] {}, new Results(R.Get(v_802, R.Call(v_65, R.Join(new V[] {R.Get(v_867, (V)"x")}, new Results(R.Get(v_867, (V)"y")))).First)));
V v_869 = values_868[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Or((V)(!R.Truth(v_869)), () => R.And(R.Or((V)(v_866 == (V)1.0d), () => (V)(v_866 == (V)R.Length(v_862))), () => R.Or((V)(v_869 == R.Get(v_849, (V)"id")), () => (V)(v_869 == R.Get(v_850, (V)"id")))))}, new Results((V)"corridor intersects authored room")));
var values_870 = R.Join(new V[] {}, new Results((V)"."));
var obj_871 = R.Get(v_798, R.Get(v_867, (V)"y"));
V key_872 = R.Get(v_867, (V)"x");
R.Set(obj_871, key_872, values_870[0]);
var values_873 = R.Join(new V[] {}, new Results(R.Get(v_779, (V)"corridor_visual")));
var obj_874 = R.Get(v_800, R.Get(v_867, (V)"y"));
V key_875 = R.Get(v_867, (V)"x");
R.Set(obj_874, key_875, values_873[0]);
if (R.Truth((V)(!R.Truth(v_869)))) {
var values_876 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_867, (V)"x")), new Entry((V)"y", R.Get(v_867, (V)"y")), new Entry((V)"kind", (V)"corridor"))));
var obj_877 = R.Get(v_790, (V)"editable_cells");
V key_878 = (V)((V)R.Length(R.Get(v_790, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_877, key_878, values_876[0]);
}
}
{ double end_881 = R.Num((V)((V)R.Length(v_862) - (V)6.0d)), step_882 = R.Num((V)8.0d);
for (double i_880 = R.Num((V)7.0d); step_882 > 0 ? i_880 <= end_881 : i_880 >= end_881; i_880 += step_882) {
V v_879 = i_880;
var values_883 = R.Join(new V[] {}, new Results(R.Get(v_862, v_879)));
V v_884 = values_883[0];
var values_885 = R.Join(new V[] {R.Get(v_862, (V)(v_879 - (V)1.0d))}, new Results(R.Get(v_862, (V)(v_879 + (V)1.0d))));
V v_886 = values_885[0];
V v_887 = values_885[1];
var values_888 = R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_886, (V)"y") == R.Get(v_884, (V)"y")), () => (V)(R.Get(v_887, (V)"y") == R.Get(v_884, (V)"y")))));
V v_889 = values_888[0];
var values_890 = R.Join(new V[] {}, new Results((V)(R.Get(v_884, (V)"x") + R.Or(R.And(v_889, () => (V)0.0d), () => R.Or(R.And((V)((V)(v_879 % (V)2.0d) == (V)0.0d), () => (V)1.0d), () => (-(V)1.0d))))));
V v_891 = values_890[0];
var values_892 = R.Join(new V[] {}, new Results((V)(R.Get(v_884, (V)"y") + R.Or(R.And(v_889, () => R.Or(R.And((V)((V)(v_879 % (V)2.0d) == (V)0.0d), () => (V)1.0d), () => (-(V)1.0d))), () => (V)0.0d))));
V v_893 = values_892[0];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_891 > (V)1.0d), () => (V)(v_891 < R.Get(v_790, (V)"width"))), () => (V)(v_893 > (V)1.0d)), () => (V)(v_893 < R.Get(v_790, (V)"height"))), () => (V)(!R.Truth(R.Get(v_802, R.Call(v_65, R.Join(new V[] {v_891}, new Results(v_893))).First)))))) {
var values_894 = R.Join(new V[] {}, new Results((V)"."));
var obj_895 = R.Get(v_798, v_893);
V key_896 = v_891;
R.Set(obj_895, key_896, values_894[0]);
var values_897 = R.Join(new V[] {}, new Results(R.Get(v_779, (V)"floor_visual")));
var obj_898 = R.Get(v_800, v_893);
V key_899 = v_891;
R.Set(obj_898, key_899, values_897[0]);
var values_900 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_891), new Entry((V)"y", v_893))));
var obj_901 = R.Get(v_864, (V)"alcoves");
V key_902 = (V)((V)R.Length(R.Get(v_864, (V)"alcoves")) + (V)1.0d);
R.Set(obj_901, key_902, values_900[0]);
var values_903 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_891), new Entry((V)"y", v_893), new Entry((V)"kind", (V)"alcove"))));
var obj_904 = R.Get(v_790, (V)"editable_cells");
V key_905 = (V)((V)R.Length(R.Get(v_790, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_904, key_905, values_903[0]);
}
}
}
if (R.Truth(R.Or((V)(R.Get(v_779, (V)"strategy") == (V)"canals"), () => (V)(R.Get(v_779, (V)"strategy") == (V)"archive")))) {
var values_906 = R.Join(new V[] {V.Nil, V.Nil}, new Results((V)0.0d));
V v_907 = values_906[0];
V v_908 = values_906[1];
V v_909 = values_906[2];
var values_910 = R.Join(new V[] {}, new Results((V)1.0d));
V v_911 = values_910[0];
{ double end_914 = R.Num((V)R.Length(v_862)), step_915 = R.Num((V)1.0d);
for (double i_913 = R.Num((V)3.0d); step_915 > 0 ? i_913 <= end_914 : i_913 >= end_914; i_913 += step_915) {
V v_912 = i_913;
var values_916 = R.Join(new V[] {R.Get(v_862, (V)(v_912 - (V)2.0d)), R.Get(v_862, (V)(v_912 - (V)1.0d))}, new Results(R.Get(v_862, v_912)));
V v_917 = values_916[0];
V v_918 = values_916[1];
V v_919 = values_916[2];
var values_920 = R.Join(new V[] {}, new Results(R.And((V)((V)(R.Get(v_918, (V)"x") - R.Get(v_917, (V)"x")) == (V)(R.Get(v_919, (V)"x") - R.Get(v_918, (V)"x"))), () => (V)((V)(R.Get(v_918, (V)"y") - R.Get(v_917, (V)"y")) == (V)(R.Get(v_919, (V)"y") - R.Get(v_918, (V)"y"))))));
V v_921 = values_920[0];
if (R.Truth(R.Or((V)(!R.Truth(v_921)), () => (V)(v_912 == (V)R.Length(v_862))))) {
var values_922 = R.Join(new V[] {}, new Results(R.Or(R.And(v_921, () => v_912), () => (V)(v_912 - (V)1.0d))));
V v_923 = values_922[0];
if (R.Truth((V)((V)((V)(v_923 - v_911) + (V)1.0d) > v_909))) {
var values_924 = R.Join(new V[] {v_911, v_923}, new Results((V)((V)(v_923 - v_911) + (V)1.0d)));
v_907 = values_924[0];
v_908 = values_924[1];
v_909 = values_924[2];
}
var values_925 = R.Join(new V[] {}, new Results((V)(v_912 - (V)1.0d)));
v_911 = values_925[0];
}
}
}
if (R.Truth((V)(v_909 >= (V)9.0d))) {
var values_926 = R.Join(new V[] {R.Get(v_862, (V)(v_907 + (V)2.0d))}, new Results(R.Get(v_862, (V)(v_908 - (V)2.0d))));
V v_927 = values_926[0];
V v_928 = values_926[1];
var values_929 = R.Join(new V[] {}, new Results((V)(R.Get(v_927, (V)"y") == R.Get(v_928, (V)"y"))));
V v_930 = values_929[0];
foreach (var row_931 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)2.0d), new Entry((V)2, (-(V)2.0d))))))))) {
V v_932 = row_931[0];
V v_933 = row_931[1];
var values_934 = R.Join(new V[] {}, new Results(R.Table()));
V v_935 = values_934[0];
V v_936 = V.Nil;
v_936 = new V((RuleFunction)(args_937 => {
V v_938 = args_937[0];
V v_939 = args_937[1];
var values_940 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_938), new Entry((V)"y", v_939))));
var obj_941 = v_935;
V key_942 = (V)((V)R.Length(v_935) + (V)1.0d);
R.Set(obj_941, key_942, values_940[0]);
return Results.Empty;
}));
{ double end_945 = R.Num((V)2.0d), step_946 = R.Num((V)1.0d);
for (double i_944 = R.Num((V)1.0d); step_946 > 0 ? i_944 <= end_945 : i_944 >= end_945; i_944 += step_946) {
V v_943 = i_944;
R.Call(v_936, R.Join(new V[] {(V)(R.Get(v_927, (V)"x") + R.Or(R.And(v_930, () => (V)0.0d), () => (V)((V)(v_933 / (V)2.0d) * v_943)))}, new Results((V)(R.Get(v_927, (V)"y") + R.Or(R.And(v_930, () => (V)((V)(v_933 / (V)2.0d) * v_943)), () => (V)0.0d)))));
}
}
var values_947 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_928, (V)"x") - R.Get(v_927, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_928, (V)"y") - R.Get(v_927, (V)"y"))))).First)));
V v_948 = values_947[0];
{ double end_951 = R.Num(v_948), step_952 = R.Num((V)1.0d);
for (double i_950 = R.Num((V)1.0d); step_952 > 0 ? i_950 <= end_951 : i_950 >= end_951; i_950 += step_952) {
V v_949 = i_950;
R.Call(v_936, R.Join(new V[] {(V)(R.Get(v_927, (V)"x") + R.Or(R.And(v_930, () => R.Or(R.And((V)(R.Get(v_928, (V)"x") > R.Get(v_927, (V)"x")), () => v_949), () => (-v_949))), () => v_933))}, new Results((V)(R.Get(v_927, (V)"y") + R.Or(R.And(v_930, () => v_933), () => R.Or(R.And((V)(R.Get(v_928, (V)"y") > R.Get(v_927, (V)"y")), () => v_949), () => (-v_949)))))));
}
}
{ double end_955 = R.Num((V)2.0d), step_956 = R.Num((V)1.0d);
for (double i_954 = R.Num((V)1.0d); step_956 > 0 ? i_954 <= end_955 : i_954 >= end_955; i_954 += step_956) {
V v_953 = i_954;
R.Call(v_936, R.Join(new V[] {(V)(R.Get(v_928, (V)"x") + R.Or(R.And(v_930, () => (V)0.0d), () => (V)(v_933 - (V)((V)(v_933 / (V)2.0d) * v_953))))}, new Results((V)(R.Get(v_928, (V)"y") + R.Or(R.And(v_930, () => (V)(v_933 - (V)((V)(v_933 / (V)2.0d) * v_953))), () => (V)0.0d)))));
}
}
var values_957 = R.Join(new V[] {}, new Results((V)true));
V v_958 = values_957[0];
foreach (var row_959 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_935)))))) {
V v_960 = row_959[0];
V v_961 = row_959[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_961, (V)"x") <= (V)1.0d), () => (V)(R.Get(v_961, (V)"x") >= R.Get(v_790, (V)"width"))), () => (V)(R.Get(v_961, (V)"y") <= (V)1.0d)), () => (V)(R.Get(v_961, (V)"y") >= R.Get(v_790, (V)"height"))), () => R.Get(v_802, R.Call(v_65, R.Join(new V[] {R.Get(v_961, (V)"x")}, new Results(R.Get(v_961, (V)"y")))).First)))) {
var values_962 = R.Join(new V[] {}, new Results((V)false));
v_958 = values_962[0];

break;
}
}
if (R.Truth(v_958)) {
foreach (var row_963 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_935)))))) {
V v_964 = row_963[0];
V v_965 = row_963[1];
var values_966 = R.Join(new V[] {}, new Results((V)"."));
var obj_967 = R.Get(v_798, R.Get(v_965, (V)"y"));
V key_968 = R.Get(v_965, (V)"x");
R.Set(obj_967, key_968, values_966[0]);
var values_969 = R.Join(new V[] {}, new Results((V)"b"));
var obj_970 = R.Get(v_800, R.Get(v_965, (V)"y"));
V key_971 = R.Get(v_965, (V)"x");
R.Set(obj_970, key_971, values_969[0]);
var values_972 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", R.Get(v_965, (V)"x")), new Entry((V)"y", R.Get(v_965, (V)"y")), new Entry((V)"kind", (V)"parallel_bank"))));
var obj_973 = R.Get(v_790, (V)"editable_cells");
V key_974 = (V)((V)R.Length(R.Get(v_790, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_973, key_974, values_972[0]);
}
var values_975 = R.Join(new V[] {}, new Results(v_935));
var obj_976 = v_864;
V key_977 = (V)"parallel";
R.Set(obj_976, key_977, values_975[0]);
break;
}
}
}
} else if (R.Truth((V)(R.Get(v_779, (V)"strategy") == (V)"courtyards"))) {
{ double end_980 = R.Num((V)((V)R.Length(v_862) - (V)5.0d)), step_981 = R.Num((V)10.0d);
for (double i_979 = R.Num((V)6.0d); step_981 > 0 ? i_979 <= end_980 : i_979 >= end_980; i_979 += step_981) {
V v_978 = i_979;
var values_982 = R.Join(new V[] {}, new Results(R.Get(v_862, v_978)));
V v_983 = values_982[0];
var values_984 = R.Join(new V[] {}, new Results(R.Table()));
V v_985 = values_984[0];
{ double end_988 = R.Num((V)1.0d), step_989 = R.Num((V)1.0d);
for (double i_987 = R.Num((-(V)1.0d)); step_989 > 0 ? i_987 <= end_988 : i_987 >= end_988; i_987 += step_989) {
V v_986 = i_987;
{ double end_992 = R.Num((V)1.0d), step_993 = R.Num((V)1.0d);
for (double i_991 = R.Num((-(V)1.0d)); step_993 > 0 ? i_991 <= end_992 : i_991 >= end_992; i_991 += step_993) {
V v_990 = i_991;
var values_994 = R.Join(new V[] {(V)(R.Get(v_983, (V)"x") + v_990)}, new Results((V)(R.Get(v_983, (V)"y") + v_986)));
V v_995 = values_994[0];
V v_996 = values_994[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_995 > (V)1.0d), () => (V)(v_995 < R.Get(v_790, (V)"width"))), () => (V)(v_996 > (V)1.0d)), () => (V)(v_996 < R.Get(v_790, (V)"height"))), () => (V)(!R.Truth(R.Get(v_802, R.Call(v_65, R.Join(new V[] {v_995}, new Results(v_996))).First)))))) {
var values_997 = R.Join(new V[] {}, new Results((V)"."));
var obj_998 = R.Get(v_798, v_996);
V key_999 = v_995;
R.Set(obj_998, key_999, values_997[0]);
var values_1000 = R.Join(new V[] {}, new Results((V)"t"));
var obj_1001 = R.Get(v_800, v_996);
V key_1002 = v_995;
R.Set(obj_1001, key_1002, values_1000[0]);
var values_1003 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_995), new Entry((V)"y", v_996), new Entry((V)"kind", (V)"courtyard_node"))));
var obj_1004 = R.Get(v_790, (V)"editable_cells");
V key_1005 = (V)((V)R.Length(R.Get(v_790, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_1004, key_1005, values_1003[0]);
var values_1006 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_995), new Entry((V)"y", v_996))));
var obj_1007 = v_985;
V key_1008 = (V)((V)R.Length(v_985) + (V)1.0d);
R.Set(obj_1007, key_1008, values_1006[0]);
}
}
}
}
}
var values_1009 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_864, (V)"widened"), () => R.Table())));
var obj_1010 = v_864;
V key_1011 = (V)"widened";
R.Set(obj_1010, key_1011, values_1009[0]);
var values_1012 = R.Join(new V[] {}, new Results(v_985));
var obj_1013 = R.Get(v_864, (V)"widened");
V key_1014 = (V)((V)R.Length(R.Get(v_864, (V)"widened")) + (V)1.0d);
R.Set(obj_1013, key_1014, values_1012[0]);
}
}
} else if (R.Truth(R.Or((V)(R.Get(v_779, (V)"strategy") == (V)"observatory"), () => (V)(R.Get(v_779, (V)"strategy") == (V)"forge")))) {
var values_1015 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_779, (V)"strategy") == (V)"observatory"), () => (V)9.0d), () => (V)11.0d)));
V v_1016 = values_1015[0];
{ double end_1019 = R.Num((V)((V)R.Length(v_862) - (V)5.0d)), step_1020 = R.Num(v_1016);
for (double i_1018 = R.Num((V)6.0d); step_1020 > 0 ? i_1018 <= end_1019 : i_1018 >= end_1019; i_1018 += step_1020) {
V v_1017 = i_1018;
var values_1021 = R.Join(new V[] {}, new Results(R.Get(v_862, v_1017)));
V v_1022 = values_1021[0];
var values_1023 = R.Join(new V[] {}, new Results(R.Table()));
V v_1024 = values_1023[0];
var values_1025 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_779, (V)"strategy") == (V)"observatory"), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))))), () => R.Or(R.And((V)((V)(v_1017 % (V)2.0d) == (V)0.0d), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (-(V)1.0d)))))), () => R.Table(new Entry((V)1, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))), new Entry((V)2, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)1.0d))), new Entry((V)3, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (-(V)1.0d)))))))));
V v_1026 = values_1025[0];
foreach (var row_1027 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1026)))))) {
V v_1028 = row_1027[0];
V v_1029 = row_1027[1];
var values_1030 = R.Join(new V[] {(V)(R.Get(v_1022, (V)"x") + R.Get(v_1029, (V)1.0d))}, new Results((V)(R.Get(v_1022, (V)"y") + R.Get(v_1029, (V)2.0d))));
V v_1031 = values_1030[0];
V v_1032 = values_1030[1];
if (R.Truth(R.And(R.And(R.And(R.And((V)(v_1031 > (V)1.0d), () => (V)(v_1031 < R.Get(v_790, (V)"width"))), () => (V)(v_1032 > (V)1.0d)), () => (V)(v_1032 < R.Get(v_790, (V)"height"))), () => (V)(!R.Truth(R.Get(v_802, R.Call(v_65, R.Join(new V[] {v_1031}, new Results(v_1032))).First)))))) {
var values_1033 = R.Join(new V[] {}, new Results((V)"."));
var obj_1034 = R.Get(v_798, v_1032);
V key_1035 = v_1031;
R.Set(obj_1034, key_1035, values_1033[0]);
var values_1036 = R.Join(new V[] {}, new Results(R.Get(v_779, (V)"floor_visual")));
var obj_1037 = R.Get(v_800, v_1032);
V key_1038 = v_1031;
R.Set(obj_1037, key_1038, values_1036[0]);
var values_1039 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_1031), new Entry((V)"y", v_1032), new Entry((V)"kind", R.Or(R.And((V)(R.Get(v_779, (V)"strategy") == (V)"observatory"), () => (V)"star_node"), () => (V)"forge_bay")))));
var obj_1040 = R.Get(v_790, (V)"editable_cells");
V key_1041 = (V)((V)R.Length(R.Get(v_790, (V)"editable_cells")) + (V)1.0d);
R.Set(obj_1040, key_1041, values_1039[0]);
var values_1042 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_1031), new Entry((V)"y", v_1032))));
var obj_1043 = v_1024;
V key_1044 = (V)((V)R.Length(v_1024) + (V)1.0d);
R.Set(obj_1043, key_1044, values_1042[0]);
}
}
var values_1045 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_864, (V)"widened"), () => R.Table())));
var obj_1046 = v_864;
V key_1047 = (V)"widened";
R.Set(obj_1046, key_1047, values_1045[0]);
var values_1048 = R.Join(new V[] {}, new Results(v_1024));
var obj_1049 = R.Get(v_864, (V)"widened");
V key_1050 = (V)((V)R.Length(R.Get(v_864, (V)"widened")) + (V)1.0d);
R.Set(obj_1049, key_1050, values_1048[0]);
}
}
}
var values_1051 = R.Join(new V[] {}, new Results(v_864));
var obj_1052 = R.Get(v_790, (V)"corridors");
V key_1053 = (V)((V)R.Length(R.Get(v_790, (V)"corridors")) + (V)1.0d);
R.Set(obj_1052, key_1053, values_1051[0]);
}
var values_1054 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_804, (V)1.0d), (V)"markers"), (V)"player"), (V)1.0d)));
V v_1055 = values_1054[0];
var values_1056 = R.Join(new V[] {}, new Results(R.Or(R.And(v_784, () => R.Get(v_784, (V)"terminal_id")), () => (V)12.0d)));
V v_1057 = values_1056[0];
var values_1058 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_804, v_1057), (V)"markers"), (V)"elite"), (V)1.0d)));
V v_1059 = values_1058[0];
var values_1060 = R.Join(new V[] {R.Get(v_1055, (V)"x")}, new Results(R.Get(v_1055, (V)"y")));
var obj_1061 = v_790;
V key_1062 = (V)"start_x";
var obj_1063 = v_790;
V key_1064 = (V)"start_y";
R.Set(obj_1061, key_1062, values_1060[0]);
R.Set(obj_1063, key_1064, values_1060[1]);
var values_1065 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"floor_id", R.Get(v_790, (V)"floor_id")), new Entry((V)"x", R.Get(v_1059, (V)"x")), new Entry((V)"y", R.Get(v_1059, (V)"y")), new Entry((V)"room_id", v_1057), new Entry((V)"kind", (V)"reserved_elite_arena"))));
var obj_1066 = v_790;
V key_1067 = (V)"elite";
R.Set(obj_1066, key_1067, values_1065[0]);
if (R.Truth(v_784)) {
var values_1068 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_781, (V)"floor_index") == (V)1.0d), () => R.Get(v_804, v_1057)), () => R.Get(v_804, (V)1.0d))));
V v_1069 = values_1068[0];
var values_1070 = R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_1069, (V)"markers"), (V)"stair"), () => R.Get(R.Get(R.Get(v_1069, (V)"markers"), (V)"stair"), (V)1.0d))));
V v_1071 = values_1070[0];
var values_1072 = R.Join(new V[] {}, new Results(R.Or(R.And(v_1071, () => R.Table(new Entry((V)"x", R.Get(v_1071, (V)"x")), new Entry((V)"y", R.Get(v_1071, (V)"y")), new Entry((V)"room_id", R.Get(v_1069, (V)"id")), new Entry((V)"template_id", R.Get(v_1069, (V)"template_id")))), () => V.Nil)));
var obj_1073 = v_790;
V key_1074 = (V)"stair_anchor";
R.Set(obj_1073, key_1074, values_1072[0]);
}
R.Call(R.Get(v_16, (V)"attach"), R.Join(new V[] {v_790, v_798, v_800, v_802, v_786}, new Results(v_779)));
R.Call(v_507, R.Join(new V[] {v_790}, new Results(v_779)));
R.Call(v_603, R.Join(new V[] {v_790, v_798, v_800, v_802, v_776}, new Results(v_777)));
var values_1075 = R.Join(new V[] {}, R.Call(v_265, R.Join(new V[] {}, new Results(v_798))));
var obj_1076 = v_790;
V key_1077 = (V)"rows";
R.Set(obj_1076, key_1077, values_1075[0]);
var values_1078 = R.Join(new V[] {}, R.Call(v_265, R.Join(new V[] {}, new Results(v_800))));
var obj_1079 = v_790;
V key_1080 = (V)"visual_rows";
R.Set(obj_1079, key_1080, values_1078[0]);
R.Call(v_704, R.Join(new V[] {v_790}, new Results(v_776)));
return R.Join(new V[] {}, new Results(v_790));
return Results.Empty;
}));
V v_1081 = V.Nil;
v_1081 = new V((RuleFunction)(args_1082 => {
V v_1083 = args_1082[0];
V v_1084 = args_1082[1];
V v_1085 = args_1082[2];
return R.Join(new V[] {}, R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_1083, v_1084}, new Results(v_1085))));
return Results.Empty;
}));
V v_1086 = V.Nil;
v_1086 = new V((RuleFunction)(args_1087 => {
V v_1088 = args_1087[0];
V v_1089 = args_1087[1];
var values_1090 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_65, R.Join(new V[] {R.Get(v_1088, (V)"start_x")}, new Results(R.Get(v_1088, (V)"start_y")))).First, (V)true))));
V v_1091 = values_1090[0];
var values_1092 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_1088, (V)"start_x")), new Entry((V)"y", R.Get(v_1088, (V)"start_y")))))));
V v_1093 = values_1092[0];
var values_1094 = R.Join(new V[] {}, new Results((V)1.0d));
V v_1095 = values_1094[0];
while (R.Truth((V)(v_1095 <= (V)R.Length(v_1093)))) {
var values_1096 = R.Join(new V[] {}, new Results(R.Get(v_1093, v_1095)));
V v_1097 = values_1096[0];

var values_1098 = R.Join(new V[] {}, new Results((V)(v_1095 + (V)1.0d)));
v_1095 = values_1098[0];
foreach (var row_1099 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_64)))))) {
V v_1100 = row_1099[0];
V v_1101 = row_1099[1];
var values_1102 = R.Join(new V[] {(V)(R.Get(v_1097, (V)"x") + R.Get(v_1101, (V)1.0d))}, new Results((V)(R.Get(v_1097, (V)"y") + R.Get(v_1101, (V)2.0d))));
V v_1103 = values_1102[0];
V v_1104 = values_1102[1];
var values_1105 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {v_1103}, new Results(v_1104))));
V v_1106 = values_1105[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_1091, v_1106))), () => R.Call(v_1081, R.Join(new V[] {v_1088, v_1103}, new Results(v_1104))).First), () => (V)(!R.Truth(R.Get(v_1089, v_1106)))))) {
var values_1107 = R.Join(new V[] {}, new Results((V)true));
var obj_1108 = v_1091;
V key_1109 = v_1106;
R.Set(obj_1108, key_1109, values_1107[0]);
var values_1110 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_1103), new Entry((V)"y", v_1104))));
var obj_1111 = v_1093;
V key_1112 = (V)((V)R.Length(v_1093) + (V)1.0d);
R.Set(obj_1111, key_1112, values_1110[0]);
}
}
}
return R.Join(new V[] {}, new Results(v_1091));
return Results.Empty;
}));
R.Set(v_18, (V)"validate", new V((RuleFunction)(args_1113 => {
V v_1114 = args_1113[0];
V v_1115 = args_1113[1];
var values_1116 = R.Join(new V[] {}, new Results(R.Table()));
V v_1117 = values_1116[0];
V v_1118 = V.Nil;
v_1118 = new V((RuleFunction)(args_1119 => {
V v_1120 = args_1119[0];
V v_1121 = args_1119[1];
V v_1122 = args_1119[2];
V v_1123 = args_1119[3];
var values_1124 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", v_1120), new Entry((V)"x", v_1121), new Entry((V)"y", v_1122), new Entry((V)"detail", v_1123))));
var obj_1125 = v_1117;
V key_1126 = (V)((V)R.Length(v_1117) + (V)1.0d);
R.Set(obj_1125, key_1126, values_1124[0]);
return Results.Empty;
}));
var values_1127 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_2, (V)"validate_map")}, new Results(v_1114))));
V v_1128 = values_1127[0];
V v_1129 = values_1127[1];
if (R.Truth((V)(!R.Truth(v_1128)))) {
R.Call(v_1118, R.Join(new V[] {(V)"map_invalid", (V)0.0d, (V)0.0d}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_1129)))));

return R.Join(new V[] {(V)false}, new Results(v_1117));
}
var values_1130 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"biome_id"), () => R.Get(R.Get(v_8, (V)"order"), (V)1.0d))))));
V v_1131 = values_1130[0];
var values_1132 = R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"layout")));
V v_1133 = values_1132[0];
if (R.Truth(R.And(v_1133, () => R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_1133, (V)"grid_cols") != (V)3.0d), () => (V)(R.Get(v_1133, (V)"grid_rows") != (V)2.0d)), () => (V)(R.Get(v_1133, (V)"room_min") != (V)6.0d)), () => (V)(R.Get(v_1133, (V)"room_max") != (V)6.0d)), () => (V)(R.Get(v_1133, (V)"terminal_id") != (V)6.0d)), () => (V)(R.Get(v_1133, (V)"width") != (V)104.0d)), () => (V)(R.Get(v_1133, (V)"height") != (V)68.0d)), () => (V)(R.Get(v_1133, (V)"base_x") != (V)20.0d)), () => (V)(R.Get(v_1133, (V)"base_y") != (V)16.0d)), () => (V)(R.Get(v_1133, (V)"slot_x") != (V)32.0d)), () => (V)(R.Get(v_1133, (V)"slot_y") != (V)26.0d)), () => R.And((V)(R.Get(v_1133, (V)"chest_target") != (V)2.0d), () => (V)(R.Get(v_1133, (V)"chest_target") != (V)3.0d))), () => R.And((V)(R.Get(v_1133, (V)"secret_kind") != (V)"cracked"), () => (V)(R.Get(v_1133, (V)"secret_kind") != (V)"sigil"))), () => R.And((V)(R.Get(v_1114, (V)"floor_index") != (V)1.0d), () => (V)(R.Get(v_1114, (V)"floor_index") != (V)2.0d)))))) {
R.Call(v_1118, R.Join(new V[] {(V)"layout_invalid", (V)0.0d, (V)0.0d}, new Results((V)"invalid connected-floor layout")));
return R.Join(new V[] {(V)false}, new Results(v_1117));
}
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(v_1131)), () => (V)(R.Get(v_1114, (V)"id") != R.Get(v_1131, (V)"map_id"))), () => R.And(R.And((V)(R.Get(v_1114, (V)"floor_id") != R.Get(v_1131, (V)"floor_id")), () => (V)(!R.Truth(R.Invoke(R.Get(v_1114, (V)"floor_id"), "match", R.Join(new V[] {}, new Results(R.Concat((V)"^", R.Concat(R.Get(v_1131, (V)"floor_id"), (V)"/stage/[2-9]$"))))).First))), () => (V)(!R.Truth(R.Invoke(R.Get(v_1114, (V)"floor_id"), "match", R.Join(new V[] {}, new Results(R.Concat((V)"^", R.Concat(R.Get(v_1131, (V)"floor_id"), (V)"/stage/[1-3]/depth/[12]$"))))).First)))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"graph")))).First != (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"rooms")))).First != (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"corridors")))).First != (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"placements")))).First != (V)"table")))) {
R.Call(v_1118, R.Join(new V[] {(V)"metadata_invalid", (V)0.0d, (V)0.0d}, new Results((V)"missing generated floor records")));
return R.Join(new V[] {(V)false}, new Results(v_1117));
}
if (R.Truth(R.Or(R.Or((V)((V)R.Length(R.Get(v_1114, (V)"rooms")) < R.Or(R.And(v_1133, () => R.Get(v_1133, (V)"room_min")), () => R.Get(v_18, (V)"room_min"))), () => (V)((V)R.Length(R.Get(v_1114, (V)"rooms")) > R.Or(R.And(v_1133, () => R.Get(v_1133, (V)"room_max")), () => R.Get(v_18, (V)"room_max")))), () => (V)((V)R.Length(R.Get(R.Get(v_1114, (V)"graph"), (V)"edges")) < (V)R.Length(R.Get(v_1114, (V)"rooms")))))) {
R.Call(v_1118, R.Join(new V[] {(V)"graph_invalid", (V)0.0d, (V)0.0d}, new Results((V)"room count or loop missing")));
}
var values_1134 = R.Join(new V[] {}, new Results(R.Table()));
V v_1135 = values_1134[0];
var values_1136 = R.Join(new V[] {}, new Results(R.Table()));
V v_1137 = values_1136[0];
foreach (var row_1138 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"rooms"))))))) {
V v_1139 = row_1138[0];
V v_1140 = row_1138[1];
var values_1141 = R.Join(new V[] {}, new Results(R.Table()));
var obj_1142 = v_1135;
V key_1143 = R.Get(v_1140, (V)"id");
R.Set(obj_1142, key_1143, values_1141[0]);
var values_1144 = R.Join(new V[] {}, new Results(v_1140));
var obj_1145 = v_1137;
V key_1146 = R.Get(v_1140, (V)"id");
R.Set(obj_1145, key_1146, values_1144[0]);
}
var values_1147 = R.Join(new V[] {}, new Results(R.Table()));
V v_1148 = values_1147[0];
foreach (var row_1149 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_1114, (V)"graph"), (V)"edges"))))))) {
V v_1150 = row_1149[0];
V v_1151 = row_1149[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Get(v_1135, R.Get(v_1151, (V)"a")))), () => (V)(!R.Truth(R.Get(v_1135, R.Get(v_1151, (V)"b"))))), () => R.Get(v_1148, R.Get(v_1151, (V)"id"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"graph_invalid", (V)0.0d, (V)0.0d}, new Results((V)"bad or duplicate edge")));
} else {
var values_1152 = R.Join(new V[] {}, new Results(R.Get(v_1151, (V)"b")));
var obj_1153 = R.Get(v_1135, R.Get(v_1151, (V)"a"));
V key_1154 = (V)((V)R.Length(R.Get(v_1135, R.Get(v_1151, (V)"a"))) + (V)1.0d);
R.Set(obj_1153, key_1154, values_1152[0]);
var values_1155 = R.Join(new V[] {}, new Results(R.Get(v_1151, (V)"a")));
var obj_1156 = R.Get(v_1135, R.Get(v_1151, (V)"b"));
V key_1157 = (V)((V)R.Length(R.Get(v_1135, R.Get(v_1151, (V)"b"))) + (V)1.0d);
R.Set(obj_1156, key_1157, values_1155[0]);
var values_1158 = R.Join(new V[] {}, new Results((V)true));
var obj_1159 = v_1148;
V key_1160 = R.Get(v_1151, (V)"id");
R.Set(obj_1159, key_1160, values_1158[0]);
}
}
var values_1161 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1.0d, (V)true))));
V v_1162 = values_1161[0];
var values_1163 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)1.0d))));
V v_1164 = values_1163[0];
{ double end_1167 = R.Num((V)R.Length(R.Get(v_1114, (V)"rooms"))), step_1168 = R.Num((V)1.0d);
for (double i_1166 = R.Num((V)1.0d); step_1168 > 0 ? i_1166 <= end_1167 : i_1166 >= end_1167; i_1166 += step_1168) {
V v_1165 = i_1166;
var values_1169 = R.Join(new V[] {}, new Results(R.Get(v_1164, v_1165)));
V v_1170 = values_1169[0];
if (R.Truth((V)(!R.Truth(v_1170)))) {
break;
}
foreach (var row_1171 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1135, v_1170), () => R.Table()))))))) {
V v_1172 = row_1171[0];
V v_1173 = row_1171[1];
if (R.Truth((V)(!R.Truth(R.Get(v_1162, v_1173))))) {
var values_1174 = R.Join(new V[] {}, new Results((V)true));
var obj_1175 = v_1162;
V key_1176 = v_1173;
R.Set(obj_1175, key_1176, values_1174[0]);
var values_1177 = R.Join(new V[] {}, new Results(v_1173));
var obj_1178 = v_1164;
V key_1179 = (V)((V)R.Length(v_1164) + (V)1.0d);
R.Set(obj_1178, key_1179, values_1177[0]);
}
}
}
}
if (R.Truth(R.Or((V)((V)R.Length(v_1164) != (V)R.Length(R.Get(v_1114, (V)"rooms"))), () => (V)(!R.Truth(R.Get(v_1162, R.Or(R.And(v_1133, () => R.Get(v_1133, (V)"terminal_id")), () => (V)12.0d))))))) {
R.Call(v_1118, R.Join(new V[] {(V)"graph_disconnected", (V)0.0d, (V)0.0d}, new Results((V)"entrance cannot reach elite room in graph")));
}
var values_1180 = R.Join(new V[] {}, new Results(R.Table()));
V v_1181 = values_1180[0];
foreach (var row_1182 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(v_1115, () => R.Table()))))))) {
V v_1183 = row_1182[0];
V v_1184 = row_1182[1];
var values_1185 = R.Join(new V[] {}, new Results(v_1184));
var obj_1186 = v_1181;
V key_1187 = R.Get(v_1184, (V)"id");
R.Set(obj_1186, key_1187, values_1185[0]);
}
var values_1188 = R.Join(new V[] {}, new Results(R.Table()));
V v_1189 = values_1188[0];
foreach (var row_1190 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"secrets"), () => R.Table()))))))) {
V v_1191 = row_1190[0];
V v_1192 = row_1190[1];
if (R.Truth(R.Get(v_1192, (V)"wall_x"))) {
var values_1193 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1181, R.Get(v_1192, (V)"id")), () => R.Get(R.Get(v_1181, R.Get(v_1192, (V)"id")), (V)"revealed"))));
var obj_1194 = v_1189;
V key_1195 = R.Call(v_65, R.Join(new V[] {R.Get(v_1192, (V)"wall_x")}, new Results(R.Get(v_1192, (V)"wall_y")))).First;
R.Set(obj_1194, key_1195, values_1193[0]);
}
}
var values_1196 = R.Join(new V[] {}, new Results(R.Table()));
V v_1197 = values_1196[0];
var values_1198 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"breakable_origins"), () => R.Table())));
V v_1199 = values_1198[0];
var values_1200 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"broken_barricades"), () => R.Table())));
V v_1201 = values_1200[0];
foreach (var row_1202 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1201)))))) {
V v_1203 = row_1202[0];
V v_1204 = row_1202[1];
if (R.Truth(R.Or((V)(v_1204 != (V)true), () => (V)(!R.Truth(R.Get(v_1199, v_1203)))))) {
R.Call(v_1118, R.Join(new V[] {(V)"barricade_state_invalid", (V)0.0d, (V)0.0d}, new Results(R.Concat((V)"unknown broken barricade ", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_1203))).First))));
}
}
foreach (var row_1205 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"protected_cells"), () => R.Table()))))))) {
V v_1206 = row_1205[0];
V v_1207 = row_1205[1];
var values_1208 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {R.Get(v_1207, (V)"x")}, new Results(R.Get(v_1207, (V)"y")))));
V v_1209 = values_1208[0];
if (R.Truth(R.And(R.Get(v_1197, v_1209), () => (V)(R.Get(v_1197, v_1209) != R.Get(v_1207, (V)"tile"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"protected_conflict", R.Get(v_1207, (V)"x"), R.Get(v_1207, (V)"y")}, new Results((V)"conflicting authored cell")));
}
var values_1210 = R.Join(new V[] {}, new Results(R.Get(v_1207, (V)"tile")));
var obj_1211 = v_1197;
V key_1212 = v_1209;
R.Set(obj_1211, key_1212, values_1210[0]);
if (R.Truth(R.And(R.And((V)(R.Invoke(R.Get(R.Get(v_1114, (V)"rows"), R.Get(v_1207, (V)"y")), "sub", R.Join(new V[] {R.Get(v_1207, (V)"x")}, new Results(R.Get(v_1207, (V)"x")))).First != R.Get(v_1207, (V)"tile")), () => (V)(!R.Truth(R.And(R.And(R.Get(v_1189, v_1209), () => (V)(R.Get(v_1207, (V)"tile") == (V)"#")), () => (V)(R.Invoke(R.Get(R.Get(v_1114, (V)"rows"), R.Get(v_1207, (V)"y")), "sub", R.Join(new V[] {R.Get(v_1207, (V)"x")}, new Results(R.Get(v_1207, (V)"x")))).First == (V)"."))))), () => (V)(!R.Truth(R.And(R.And(R.Get(v_1201, v_1209), () => (V)(R.Get(v_1207, (V)"tile") == (V)"B")), () => (V)(R.Invoke(R.Get(R.Get(v_1114, (V)"rows"), R.Get(v_1207, (V)"y")), "sub", R.Join(new V[] {R.Get(v_1207, (V)"x")}, new Results(R.Get(v_1207, (V)"x")))).First == (V)"."))))))) {
R.Call(v_1118, R.Join(new V[] {(V)"protected_changed", R.Get(v_1207, (V)"x"), R.Get(v_1207, (V)"y")}, new Results((V)"authored geometry changed")));
}
}
if (R.Truth(R.And(v_1133, () => R.Get(v_1114, (V)"terrain_placements")))) {
var values_1213 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Get(v_1114, (V)"floor_index") == (V)1.0d), () => R.Table(new Entry((V)"b", (V)true), new Entry((V)"g", (V)true), new Entry((V)"i", (V)true))), () => R.Table(new Entry((V)"B", (V)true), new Entry((V)"m", (V)true), new Entry((V)"c", (V)true)))));
V v_1214 = values_1213[0];
var values_1215 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_1216 = values_1215[0];
V v_1217 = values_1215[1];
foreach (var row_1218 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"terrain_placements"), () => R.Table()))))))) {
V v_1219 = row_1218[0];
V v_1220 = row_1218[1];
var values_1221 = R.Join(new V[] {}, new Results(R.Get(v_1220, (V)"symbol")));
V v_1222 = values_1221[0];
var values_1223 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {R.Get(v_1220, (V)"x")}, new Results(R.Get(v_1220, (V)"y")))));
V v_1224 = values_1223[0];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(R.Get(v_1214, v_1222))), () => R.Get(v_1216, v_1222)), () => R.Get(v_1217, v_1224)), () => (V)(!R.Truth(R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_1222))).First))), () => (V)(R.Get(v_1220, (V)"id") != R.Get(R.Call(R.Get(v_14, (V)"get"), R.Join(new V[] {}, new Results(v_1222))).First, (V)"id"))), () => (V)(R.Call(R.Get(v_14, (V)"symbol"), R.Join(new V[] {v_1114, R.Get(v_1220, (V)"x")}, new Results(R.Get(v_1220, (V)"y")))).First != R.Or(R.And(R.Get(v_1201, v_1224), () => (V)"."), () => v_1222))))) {
R.Call(v_1118, R.Join(new V[] {(V)"terrain_placement_invalid", R.Get(v_1220, (V)"x"), R.Get(v_1220, (V)"y")}, R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_1222)))));
}
var values_1225 = R.Join(new V[] {}, new Results((V)true));
var obj_1226 = v_1216;
V key_1227 = v_1222;
R.Set(obj_1226, key_1227, values_1225[0]);

var values_1228 = R.Join(new V[] {}, new Results((V)true));
var obj_1229 = v_1217;
V key_1230 = v_1224;
R.Set(obj_1229, key_1230, values_1228[0]);
}
foreach (var row_1231 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1214)))))) {
V v_1232 = row_1231[0];
if (R.Truth((V)(!R.Truth(R.Get(v_1216, v_1232))))) {
R.Call(v_1118, R.Join(new V[] {(V)"terrain_placement_missing", (V)0.0d, (V)0.0d}, new Results(v_1232)));
}
}
}
var values_1233 = R.Join(new V[] {}, new Results(R.Table()));
V v_1234 = values_1233[0];
foreach (var row_1235 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"editable_cells"), () => R.Table()))))))) {
V v_1236 = row_1235[0];
V v_1237 = row_1235[1];
var values_1238 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {R.Get(v_1237, (V)"x")}, new Results(R.Get(v_1237, (V)"y")))));
V v_1239 = values_1238[0];
var values_1240 = R.Join(new V[] {}, new Results((V)true));
var obj_1241 = v_1234;
V key_1242 = v_1239;
R.Set(obj_1241, key_1242, values_1240[0]);
if (R.Truth(R.Get(v_1197, v_1239))) {
R.Call(v_1118, R.Join(new V[] {(V)"editable_protected_overlap", R.Get(v_1237, (V)"x"), R.Get(v_1237, (V)"y")}, new Results((V)"mask conflict")));
}
}
var values_1243 = R.Join(new V[] {}, new Results(R.Table()));
V v_1244 = values_1243[0];
foreach (var row_1245 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"rooms"))))))) {
V v_1246 = row_1245[0];
V v_1247 = row_1245[1];
var values_1248 = R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_6, (V)"definitions"), R.Get(v_1247, (V)"template_id")), () => R.And(R.Get(v_1114, (V)"studio_templates"), () => R.Get(R.Get(v_1114, (V)"studio_templates"), R.Get(v_1247, (V)"template_id"))))));
V v_1249 = values_1248[0];
var values_1250 = Results.Empty;
V v_1251 = values_1250[0];
if (R.Truth(v_1249)) {
var values_1252 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_6, (V)"transform"), v_1249, R.Get(v_1247, (V)"turns")}, new Results(R.Get(v_1247, (V)"mirror")))));
V v_1253 = values_1252[0];
V v_1254 = values_1252[1];
if (R.Truth(v_1253)) {
var values_1255 = R.Join(new V[] {}, new Results(v_1254));
v_1251 = values_1255[0];
}
}
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_1251)), () => (V)(R.Get(v_1247, (V)"width") != R.Get(v_1251, (V)"width"))), () => (V)(R.Get(v_1247, (V)"height") != R.Get(v_1251, (V)"height"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"room_invalid", R.Get(v_1247, (V)"x"), R.Get(v_1247, (V)"y")}, new Results((V)"template or transform mismatch")));
} else {
var values_1256 = R.Join(new V[] {}, new Results(R.Table()));
V v_1257 = values_1256[0];
foreach (var row_1258 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1251, (V)"editable"))))))) {
V v_1259 = row_1258[0];
V v_1260 = row_1258[1];
var values_1261 = R.Join(new V[] {}, new Results((V)true));
var obj_1262 = v_1257;
V key_1263 = R.Call(v_65, R.Join(new V[] {R.Get(v_1260, (V)"x")}, new Results(R.Get(v_1260, (V)"y")))).First;
R.Set(obj_1262, key_1263, values_1261[0]);
}
{ double end_1266 = R.Num(R.Get(v_1247, (V)"height")), step_1267 = R.Num((V)1.0d);
for (double i_1265 = R.Num((V)1.0d); step_1267 > 0 ? i_1265 <= end_1266 : i_1265 >= end_1266; i_1265 += step_1267) {
V v_1264 = i_1265;
{ double end_1270 = R.Num(R.Get(v_1247, (V)"width")), step_1271 = R.Num((V)1.0d);
for (double i_1269 = R.Num((V)1.0d); step_1271 > 0 ? i_1269 <= end_1270 : i_1269 >= end_1270; i_1269 += step_1271) {
V v_1268 = i_1269;
var values_1272 = R.Join(new V[] {(V)((V)(R.Get(v_1247, (V)"x") + v_1268) - (V)1.0d)}, new Results((V)((V)(R.Get(v_1247, (V)"y") + v_1264) - (V)1.0d)));
V v_1273 = values_1272[0];
V v_1274 = values_1272[1];
var values_1275 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {v_1273}, new Results(v_1274))));
V v_1276 = values_1275[0];
var values_1277 = R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_1251, (V)"rows"), v_1264), "sub", R.Join(new V[] {v_1268}, new Results(v_1268))));
V v_1278 = values_1277[0];
if (R.Truth((V)(v_1278 != (V)" "))) {
if (R.Truth(R.Get(v_1244, v_1276))) {
R.Call(v_1118, R.Join(new V[] {(V)"room_overlap", v_1273, v_1274}, new Results(R.Get(v_1247, (V)"id"))));
}
var values_1279 = R.Join(new V[] {}, new Results(R.Get(v_1247, (V)"id")));
var obj_1280 = v_1244;
V key_1281 = v_1276;
R.Set(obj_1280, key_1281, values_1279[0]);
if (R.Truth(R.And((V)(!R.Truth(R.Get(v_1257, R.Call(v_65, R.Join(new V[] {v_1268}, new Results(v_1264))).First))), () => (V)(R.Get(v_1197, v_1276) != v_1278)))) {
R.Call(v_1118, R.Join(new V[] {(V)"protected_mask_missing", v_1273, v_1274}, new Results(R.Get(v_1247, (V)"template_id"))));
}
}
}
}
}
}
foreach (var row_1282 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1251, (V)"sockets"))))))) {
V v_1283 = row_1282[0];
V v_1284 = row_1282[1];
var values_1285 = R.Join(new V[] {(V)((V)(R.Get(v_1247, (V)"x") + R.Get(v_1284, (V)"x")) - (V)1.0d)}, new Results((V)((V)(R.Get(v_1247, (V)"y") + R.Get(v_1284, (V)"y")) - (V)1.0d)));
V v_1286 = values_1285[0];
V v_1287 = values_1285[1];
var values_1288 = R.Join(new V[] {}, new Results((V)false));
V v_1289 = values_1288[0];
foreach (var row_1290 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1247, (V)"sockets"))))))) {
V v_1291 = row_1290[0];
V v_1292 = row_1290[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_1292, (V)"x") == v_1286), () => (V)(R.Get(v_1292, (V)"y") == v_1287)), () => (V)(R.Get(v_1292, (V)"facing") == R.Get(v_1284, (V)"facing"))), () => (V)(R.Get(v_1292, (V)"width") == R.Get(v_1284, (V)"width"))))) {
var values_1293 = R.Join(new V[] {}, new Results((V)true));
v_1289 = values_1293[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_1289)))) {
R.Call(v_1118, R.Join(new V[] {(V)"socket_invalid", v_1286, v_1287}, new Results(R.Get(v_1247, (V)"id"))));
}
}
foreach (var row_1294 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_1251, (V)"markers"))))))) {
V v_1295 = row_1294[0];
V v_1296 = row_1294[1];
foreach (var row_1297 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1296)))))) {
V v_1298 = row_1297[0];
V v_1299 = row_1297[1];
var values_1300 = R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_1247, (V)"markers"), v_1295), () => R.Get(R.Get(R.Get(v_1247, (V)"markers"), v_1295), v_1298))));
V v_1301 = values_1300[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_1301)), () => (V)(R.Get(v_1301, (V)"x") != (V)((V)(R.Get(v_1247, (V)"x") + R.Get(v_1299, (V)"x")) - (V)1.0d))), () => (V)(R.Get(v_1301, (V)"y") != (V)((V)(R.Get(v_1247, (V)"y") + R.Get(v_1299, (V)"y")) - (V)1.0d))))) {
R.Call(v_1118, R.Join(new V[] {(V)"marker_invalid", (V)((V)(R.Get(v_1247, (V)"x") + R.Get(v_1299, (V)"x")) - (V)1.0d), (V)((V)(R.Get(v_1247, (V)"y") + R.Get(v_1299, (V)"y")) - (V)1.0d)}, new Results(v_1295)));
}
}
}
}
}
if (R.Truth((V)((V)R.Length(R.Get(v_1114, (V)"corridors")) != (V)R.Length(R.Get(R.Get(v_1114, (V)"graph"), (V)"edges"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_count", (V)0.0d, (V)0.0d}, new Results((V)"graph edge lacks concrete corridor")));
}
var values_1302 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"N", (V)"S"), new Entry((V)"S", (V)"N"), new Entry((V)"E", (V)"W"), new Entry((V)"W", (V)"E"))));
V v_1303 = values_1302[0];
V v_1304 = V.Nil;
v_1304 = new V((RuleFunction)(args_1305 => {
V v_1306 = args_1305[0];
V v_1307 = args_1305[1];
foreach (var row_1308 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1306, (V)"sockets"))))))) {
V v_1309 = row_1308[0];
V v_1310 = row_1308[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_1310, (V)"x") == R.Get(v_1307, (V)"x")), () => (V)(R.Get(v_1310, (V)"y") == R.Get(v_1307, (V)"y"))), () => (V)(R.Get(v_1310, (V)"facing") == R.Get(v_1307, (V)"facing"))))) {
return R.Join(new V[] {}, new Results((V)true));
}
}
return R.Join(new V[] {}, new Results((V)false));
return Results.Empty;
}));
foreach (var row_1311 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"corridors"))))))) {
V v_1312 = row_1311[0];
V v_1313 = row_1311[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Get(v_1148, R.Get(v_1313, (V)"edge_id")))), () => (V)(!R.Truth(R.Get(v_1137, R.Get(v_1313, (V)"from_room"))))), () => (V)(!R.Truth(R.Get(v_1137, R.Get(v_1313, (V)"to_room"))))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_invalid", (V)0.0d, (V)0.0d}, new Results((V)"unknown graph connection")));
} else if (R.Truth(R.Or(R.Or((V)(!R.Truth(R.Call(v_1304, R.Join(new V[] {R.Get(v_1137, R.Get(v_1313, (V)"from_room"))}, new Results(R.Get(v_1313, (V)"from_socket")))).First)), () => (V)(!R.Truth(R.Call(v_1304, R.Join(new V[] {R.Get(v_1137, R.Get(v_1313, (V)"to_room"))}, new Results(R.Get(v_1313, (V)"to_socket")))).First))), () => (V)(R.Get(v_1303, R.Get(R.Get(v_1313, (V)"from_socket"), (V)"facing")) != R.Get(R.Get(v_1313, (V)"to_socket"), (V)"facing"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"socket_mismatch", R.Get(R.Get(v_1313, (V)"from_socket"), (V)"x"), R.Get(R.Get(v_1313, (V)"from_socket"), (V)"y")}, new Results(R.Get(v_1313, (V)"edge_id"))));
}
var values_1314 = R.Join(new V[] {R.Get(R.Get(v_1313, (V)"cells"), (V)1.0d)}, new Results(R.Get(R.Get(v_1313, (V)"cells"), (V)R.Length(R.Get(v_1313, (V)"cells")))));
V v_1315 = values_1314[0];
V v_1316 = values_1314[1];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_1315)), () => (V)(!R.Truth(v_1316))), () => (V)(!R.Truth(R.Or(R.And(R.And(R.And((V)(R.Get(v_1315, (V)"x") == R.Get(R.Get(v_1313, (V)"from_socket"), (V)"x")), () => (V)(R.Get(v_1315, (V)"y") == R.Get(R.Get(v_1313, (V)"from_socket"), (V)"y"))), () => (V)(R.Get(v_1316, (V)"x") == R.Get(R.Get(v_1313, (V)"to_socket"), (V)"x"))), () => (V)(R.Get(v_1316, (V)"y") == R.Get(R.Get(v_1313, (V)"to_socket"), (V)"y"))), () => R.And(R.And(R.And((V)(R.Get(v_1316, (V)"x") == R.Get(R.Get(v_1313, (V)"from_socket"), (V)"x")), () => (V)(R.Get(v_1316, (V)"y") == R.Get(R.Get(v_1313, (V)"from_socket"), (V)"y"))), () => (V)(R.Get(v_1315, (V)"x") == R.Get(R.Get(v_1313, (V)"to_socket"), (V)"x"))), () => (V)(R.Get(v_1315, (V)"y") == R.Get(R.Get(v_1313, (V)"to_socket"), (V)"y"))))))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_endpoint", (V)0.0d, (V)0.0d}, new Results(R.Get(v_1313, (V)"edge_id"))));
}
foreach (var row_1317 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1313, (V)"cells"))))))) {
V v_1318 = row_1317[0];
V v_1319 = row_1317[1];
if (R.Truth((V)(!R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, R.Get(v_1319, (V)"x")}, new Results(R.Get(v_1319, (V)"y")))).First)))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_blocked", R.Get(v_1319, (V)"x"), R.Get(v_1319, (V)"y")}, new Results(R.Get(v_1313, (V)"edge_id"))));
}
var values_1320 = R.Join(new V[] {}, new Results(R.Get(v_1244, R.Call(v_65, R.Join(new V[] {R.Get(v_1319, (V)"x")}, new Results(R.Get(v_1319, (V)"y")))).First)));
V v_1321 = values_1320[0];
if (R.Truth(R.And(R.And(v_1321, () => (V)(v_1318 != (V)1.0d)), () => (V)(v_1318 != (V)R.Length(R.Get(v_1313, (V)"cells")))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_room_crossing", R.Get(v_1319, (V)"x"), R.Get(v_1319, (V)"y")}, new Results(R.Get(v_1313, (V)"edge_id"))));
}
if (R.Truth((V)(v_1318 > (V)1.0d))) {
var values_1322 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1313, (V)"cells"), (V)(v_1318 - (V)1.0d))));
V v_1323 = values_1322[0];
if (R.Truth((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_1319, (V)"x") - R.Get(v_1323, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_1319, (V)"y") - R.Get(v_1323, (V)"y"))))).First) != (V)1.0d))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_gap", R.Get(v_1319, (V)"x"), R.Get(v_1319, (V)"y")}, new Results(R.Get(v_1313, (V)"edge_id"))));
}
}
}
var values_1324 = R.Join(new V[] {R.Get(v_1313, (V)"from_socket")}, new Results(R.Get(v_1313, (V)"to_socket")));
V v_1325 = values_1324[0];
V v_1326 = values_1324[1];
if (R.Truth(R.Or((V)(!R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, R.Get(v_1325, (V)"x")}, new Results(R.Get(v_1325, (V)"y")))).First)), () => (V)(!R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, R.Get(v_1326, (V)"x")}, new Results(R.Get(v_1326, (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"socket_blocked", R.Get(v_1325, (V)"x"), R.Get(v_1325, (V)"y")}, new Results(R.Get(v_1313, (V)"edge_id"))));
}
}
if (R.Truth(R.Get(v_1114, (V)"corridor_quests"))) {
if (R.Truth(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"corridor_quests")))).First != (V)"table"), () => (V)((V)R.Length(R.Get(v_1114, (V)"corridor_quests")) > (V)1.0d)))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_count", (V)0.0d, (V)0.0d}, new Results((V)"at most one branch quest per floor")));
} else {
var values_1327 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"spur", (V)true), new Entry((V)"braid", (V)true), new Entry((V)"switchback", (V)true), new Entry((V)"hub", (V)true), new Entry((V)"false_end", (V)true))));
V v_1328 = values_1327[0];
var values_1329 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"spur", R.Table(new Entry((V)"clue", (V)1.0d), new Entry((V)"reward", (V)1.0d))), new Entry((V)"braid", R.Table(new Entry((V)"key", (V)1.0d), new Entry((V)"gate", (V)1.0d), new Entry((V)"reward", (V)1.0d))), new Entry((V)"switchback", R.Table(new Entry((V)"plate", (V)1.0d), new Entry((V)"gate", (V)1.0d), new Entry((V)"reward", (V)1.0d))), new Entry((V)"hub", R.Table(new Entry((V)"brazier", (V)2.0d), new Entry((V)"reward", (V)1.0d))), new Entry((V)"false_end", R.Table(new Entry((V)"clue", (V)1.0d), new Entry((V)"gate", (V)1.0d), new Entry((V)"enemy", (V)1.0d), new Entry((V)"reward", (V)1.0d))))));
V v_1330 = values_1329[0];
foreach (var row_1331 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"corridor_quests"))))))) {
V v_1332 = row_1331[0];
V v_1333 = row_1331[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or((V)(!R.Truth(R.Get(v_1328, R.Get(v_1333, (V)"kind")))), () => (V)(!R.Truth(R.Get(v_1148, R.Get(v_1333, (V)"edge_id"))))), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1333, (V)"nodes")))).First != (V)"table")), () => (V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(R.Get(v_1333, (V)"cells")))).First != (V)"table")), () => (V)((V)R.Length(R.Get(v_1333, (V)"cells")) < (V)3.0d)))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_invalid", (V)0.0d, (V)0.0d}, new Results(R.Get(v_1333, (V)"id"))));
} else {
var values_1334 = R.Join(new V[] {}, new Results(R.Table()));
V v_1335 = values_1334[0];
foreach (var row_1336 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1333, (V)"cells"))))))) {
V v_1337 = row_1336[0];
V v_1338 = row_1336[1];
var values_1339 = R.Join(new V[] {}, new Results((V)true));
var obj_1340 = v_1335;
V key_1341 = R.Call(v_65, R.Join(new V[] {R.Get(v_1338, (V)"x")}, new Results(R.Get(v_1338, (V)"y")))).First;
R.Set(obj_1340, key_1341, values_1339[0]);
if (R.Truth(R.Or((V)(!R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, R.Get(v_1338, (V)"x")}, new Results(R.Get(v_1338, (V)"y")))).First)), () => R.Get(v_1244, R.Call(v_65, R.Join(new V[] {R.Get(v_1338, (V)"x")}, new Results(R.Get(v_1338, (V)"y")))).First)))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_blocked", R.Get(v_1338, (V)"x"), R.Get(v_1338, (V)"y")}, new Results(R.Get(v_1333, (V)"id"))));
}
}
var values_1342 = R.Join(new V[] {}, new Results(R.Table()));
V v_1343 = values_1342[0];
foreach (var row_1344 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1333, (V)"nodes"))))))) {
V v_1345 = row_1344[0];
V v_1346 = row_1344[1];
var values_1347 = R.Join(new V[] {}, new Results((V)(R.Or(R.Get(v_1343, R.Get(v_1346, (V)"kind")), () => (V)0.0d) + (V)1.0d)));
var obj_1348 = v_1343;
V key_1349 = R.Get(v_1346, (V)"kind");
R.Set(obj_1348, key_1349, values_1347[0]);
if (R.Truth(R.Or((V)(!R.Truth(R.Get(v_1335, R.Call(v_65, R.Join(new V[] {R.Get(v_1346, (V)"x")}, new Results(R.Get(v_1346, (V)"y")))).First))), () => (V)(!R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, R.Get(v_1346, (V)"x")}, new Results(R.Get(v_1346, (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_node", R.Get(v_1346, (V)"x"), R.Get(v_1346, (V)"y")}, new Results(R.Get(v_1333, (V)"id"))));
}
}
foreach (var row_1350 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(R.Get(v_1330, R.Get(v_1333, (V)"kind")))))))) {
V v_1351 = row_1350[0];
V v_1352 = row_1350[1];
if (R.Truth((V)(R.Get(v_1343, v_1351) != v_1352))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_node_count", (V)0.0d, (V)0.0d}, new Results(R.Concat(R.Get(v_1333, (V)"id"), R.Concat((V)":", v_1351)))));
}
}
foreach (var row_1353 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1343)))))) {
V v_1354 = row_1353[0];
if (R.Truth((V)(!R.Truth(R.Get(R.Get(v_1330, R.Get(v_1333, (V)"kind")), v_1354))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_node_kind", (V)0.0d, (V)0.0d}, new Results(R.Concat(R.Get(v_1333, (V)"id"), R.Concat((V)":", v_1354)))));
}
}
}
}
}
}
var values_1355 = R.Join(new V[] {}, new Results(R.Table()));
V v_1356 = values_1355[0];
var values_1357 = R.Join(new V[] {}, new Results(R.Table()));
V v_1358 = values_1357[0];
var values_1359 = R.Join(new V[] {}, new Results(R.Table()));
V v_1360 = values_1359[0];
var values_1361 = R.Join(new V[] {}, new Results((V)true));
var obj_1362 = v_1360;
V key_1363 = R.Call(v_65, R.Join(new V[] {R.Get(v_1114, (V)"start_x")}, new Results(R.Get(v_1114, (V)"start_y")))).First;
R.Set(obj_1362, key_1363, values_1361[0]);
foreach (var row_1364 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"chests"), new Entry((V)2, (V)"enemies"), new Entry((V)3, (V)"doors")))))))) {
V v_1365 = row_1364[0];
V v_1366 = row_1364[1];
foreach (var row_1367 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"placements"), v_1366), () => R.Table()))))))) {
V v_1368 = row_1367[0];
V v_1369 = row_1367[1];
var values_1370 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {R.Get(v_1369, (V)"x")}, new Results(R.Get(v_1369, (V)"y")))));
V v_1371 = values_1370[0];
if (R.Truth(R.Or(R.Or(R.Get(v_1358, R.Get(v_1369, (V)"id")), () => R.Get(v_1360, v_1371)), () => (V)(!R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, R.Get(v_1369, (V)"x")}, new Results(R.Get(v_1369, (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"placement_invalid", R.Get(v_1369, (V)"x"), R.Get(v_1369, (V)"y")}, new Results(R.Get(v_1369, (V)"id"))));
}
var values_1372 = R.Join(new V[] {}, new Results((V)true));
var obj_1373 = v_1358;
V key_1374 = R.Get(v_1369, (V)"id");
R.Set(obj_1373, key_1374, values_1372[0]);

var values_1375 = R.Join(new V[] {}, new Results((V)true));
var obj_1376 = v_1360;
V key_1377 = v_1371;
R.Set(obj_1376, key_1377, values_1375[0]);
if (R.Truth((V)(v_1366 == (V)"chests"))) {
var values_1378 = R.Join(new V[] {}, new Results((V)true));
var obj_1379 = v_1356;
V key_1380 = v_1371;
R.Set(obj_1379, key_1380, values_1378[0]);
}
if (R.Truth(R.And((V)(v_1366 == (V)"enemies"), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_1369, (V)"x") - R.Get(v_1114, (V)"start_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_1369, (V)"y") - R.Get(v_1114, (V)"start_y"))))).First) < (V)5.0d)))) {
R.Call(v_1118, R.Join(new V[] {(V)"start_unsafe", R.Get(v_1369, (V)"x"), R.Get(v_1369, (V)"y")}, new Results(R.Get(v_1369, (V)"id"))));
}
}
}
if (R.Truth((V)((V)R.Length(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"chests"), () => R.Table())) != R.Or(R.And(v_1133, () => R.Get(v_1133, (V)"chest_target")), () => R.Get(v_18, (V)"chest_target"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"chest_count", (V)0.0d, (V)0.0d}, new Results((V)"wrong paid chest count")));
}
var values_1381 = R.Join(new V[] {}, R.Call(v_1086, R.Join(new V[] {v_1114}, new Results(v_1356))));
V v_1382 = values_1381[0];
foreach (var row_1383 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1114, (V)"corridor_quests"), () => R.Table()))))))) {
V v_1384 = row_1383[0];
V v_1385 = row_1383[1];
foreach (var row_1386 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1385, (V)"cells"))))))) {
V v_1387 = row_1386[0];
V v_1388 = row_1386[1];
if (R.Truth((V)(!R.Truth(R.Get(v_1382, R.Call(v_65, R.Join(new V[] {R.Get(v_1388, (V)"x")}, new Results(R.Get(v_1388, (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"corridor_quest_unreachable", R.Get(v_1388, (V)"x"), R.Get(v_1388, (V)"y")}, new Results(R.Get(v_1385, (V)"id"))));
}
}
}
if (R.Truth((V)(!R.Truth(R.Get(v_1382, R.Call(v_65, R.Join(new V[] {R.Get(R.Get(v_1114, (V)"elite"), (V)"x")}, new Results(R.Get(R.Get(v_1114, (V)"elite"), (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"elite_unreachable", R.Get(R.Get(v_1114, (V)"elite"), (V)"x"), R.Get(R.Get(v_1114, (V)"elite"), (V)"y")}, new Results((V)"no cardinal route")));
}
foreach (var row_1389 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"rooms"))))))) {
V v_1390 = row_1389[0];
V v_1391 = row_1389[1];
if (R.Truth((V)(!R.Truth(R.Get(v_1382, R.Call(v_65, R.Join(new V[] {R.Get(R.Get(v_1391, (V)"center"), (V)"x")}, new Results(R.Get(R.Get(v_1391, (V)"center"), (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"room_unreachable", R.Get(R.Get(v_1391, (V)"center"), (V)"x"), R.Get(R.Get(v_1391, (V)"center"), (V)"y")}, new Results(R.Get(v_1391, (V)"id"))));
}
var values_1392 = R.Join(new V[] {}, new Results((V)0.0d));
V v_1393 = values_1392[0];
foreach (var row_1394 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_64)))))) {
V v_1395 = row_1394[0];
V v_1396 = row_1394[1];
if (R.Truth(R.Call(v_1081, R.Join(new V[] {v_1114, (V)(R.Get(R.Get(v_1391, (V)"center"), (V)"x") + R.Get(v_1396, (V)1.0d))}, new Results((V)(R.Get(R.Get(v_1391, (V)"center"), (V)"y") + R.Get(v_1396, (V)2.0d))))).First)) {
var values_1397 = R.Join(new V[] {}, new Results((V)(v_1393 + (V)1.0d)));
v_1393 = values_1397[0];
}
}
if (R.Truth(R.Or((V)(v_1393 < (V)2.0d), () => R.And(R.Or((V)(R.Get(v_1391, (V)"role") == (V)"start"), () => (V)(R.Get(v_1391, (V)"role") == (V)"elite")), () => (V)(v_1393 < (V)4.0d))))) {
R.Call(v_1118, R.Join(new V[] {(V)"combat_clearance", R.Get(R.Get(v_1391, (V)"center"), (V)"x"), R.Get(R.Get(v_1391, (V)"center"), (V)"y")}, new Results(R.Get(v_1391, (V)"id"))));
}
if (R.Truth(R.Or((V)(!R.Truth(R.Get(R.Get(v_1391, (V)"markers"), (V)"secret_anchor"))), () => (V)((V)R.Length(R.Get(R.Get(v_1391, (V)"markers"), (V)"secret_anchor")) == (V)0.0d)))) {
R.Call(v_1118, R.Join(new V[] {(V)"future_anchor_missing", R.Get(R.Get(v_1391, (V)"center"), (V)"x"), R.Get(R.Get(v_1391, (V)"center"), (V)"y")}, new Results(R.Get(v_1391, (V)"id"))));
}
}
foreach (var row_1398 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"chests"), () => R.Table()))))))) {
V v_1399 = row_1398[0];
V v_1400 = row_1398[1];
var values_1401 = R.Join(new V[] {}, new Results((V)false));
V v_1402 = values_1401[0];
foreach (var row_1403 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_64)))))) {
V v_1404 = row_1403[0];
V v_1405 = row_1403[1];
if (R.Truth(R.Get(v_1382, R.Call(v_65, R.Join(new V[] {(V)(R.Get(v_1400, (V)"x") + R.Get(v_1405, (V)1.0d))}, new Results((V)(R.Get(v_1400, (V)"y") + R.Get(v_1405, (V)2.0d))))).First))) {
var values_1406 = R.Join(new V[] {}, new Results((V)true));
v_1402 = values_1406[0];
}
}
if (R.Truth((V)(!R.Truth(v_1402)))) {
R.Call(v_1118, R.Join(new V[] {(V)"chest_unreachable", R.Get(v_1400, (V)"x"), R.Get(v_1400, (V)"y")}, new Results(R.Get(v_1400, (V)"id"))));
}
}
var values_1407 = R.Join(new V[] {}, new Results(R.Table()));
V v_1408 = values_1407[0];
foreach (var row_1409 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_1356)))))) {
V v_1410 = row_1409[0];
V v_1411 = row_1409[1];
var values_1412 = R.Join(new V[] {}, new Results(v_1411));
var obj_1413 = v_1408;
V key_1414 = v_1410;
R.Set(obj_1413, key_1414, values_1412[0]);
}
foreach (var row_1415 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"doors"), () => R.Table()))))))) {
V v_1416 = row_1415[0];
V v_1417 = row_1415[1];
var values_1418 = R.Join(new V[] {}, new Results((V)true));
var obj_1419 = v_1408;
V key_1420 = R.Call(v_65, R.Join(new V[] {R.Get(v_1417, (V)"x")}, new Results(R.Get(v_1417, (V)"y")))).First;
R.Set(obj_1419, key_1420, values_1418[0]);
var values_1421 = R.Join(new V[] {}, new Results(R.Get(v_1137, R.Get(v_1417, (V)"room_id"))));
V v_1422 = values_1421[0];
var values_1423 = R.Join(new V[] {}, new Results((V)false));
V v_1424 = values_1423[0];
foreach (var row_1425 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(v_1422, () => R.Get(v_1422, (V)"sockets")), () => R.Table()))))))) {
V v_1426 = row_1425[0];
V v_1427 = row_1425[1];
if (R.Truth(R.And((V)(R.Get(v_1427, (V)"x") == R.Get(v_1417, (V)"x")), () => (V)(R.Get(v_1427, (V)"y") == R.Get(v_1417, (V)"y"))))) {
var values_1428 = R.Join(new V[] {}, new Results((V)true));
v_1424 = values_1428[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_1424)))) {
R.Call(v_1118, R.Join(new V[] {(V)"door_not_socket", R.Get(v_1417, (V)"x"), R.Get(v_1417, (V)"y")}, new Results(R.Get(v_1417, (V)"id"))));
}
var values_1429 = R.Join(new V[] {}, new Results((V)false));
V v_1430 = values_1429[0];
foreach (var row_1431 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"graph"), (V)"critical"), () => R.Table()))))))) {
V v_1432 = row_1431[0];
V v_1433 = row_1431[1];
if (R.Truth((V)(v_1433 == R.Get(v_1417, (V)"room_id")))) {
var values_1434 = R.Join(new V[] {}, new Results((V)true));
v_1430 = values_1434[0];
}
}
if (R.Truth(v_1430)) {
R.Call(v_1118, R.Join(new V[] {(V)"door_on_critical_route", R.Get(v_1417, (V)"x"), R.Get(v_1417, (V)"y")}, new Results(R.Get(v_1417, (V)"id"))));
}
}
if (R.Truth((V)((V)R.Length(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"doors"), () => R.Table())) != (V)1.0d))) {
R.Call(v_1118, R.Join(new V[] {(V)"door_count", (V)0.0d, (V)0.0d}, new Results((V)"expected one optional unlocked door")));
}
var values_1435 = R.Join(new V[] {}, R.Call(v_1086, R.Join(new V[] {v_1114}, new Results(v_1408))));
V v_1436 = values_1435[0];
if (R.Truth((V)(!R.Truth(R.Get(v_1436, R.Call(v_65, R.Join(new V[] {R.Get(R.Get(v_1114, (V)"elite"), (V)"x")}, new Results(R.Get(R.Get(v_1114, (V)"elite"), (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"closed_door_blocks_route", R.Get(R.Get(v_1114, (V)"elite"), (V)"x"), R.Get(R.Get(v_1114, (V)"elite"), (V)"y")}, new Results((V)"elite route blocked")));
}
foreach (var row_1437 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"doors"), () => R.Table()))))))) {
V v_1438 = row_1437[0];
V v_1439 = row_1437[1];
var values_1440 = R.Join(new V[] {}, new Results((V)false));
V v_1441 = values_1440[0];
foreach (var row_1442 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_64)))))) {
V v_1443 = row_1442[0];
V v_1444 = row_1442[1];
if (R.Truth(R.Get(v_1436, R.Call(v_65, R.Join(new V[] {(V)(R.Get(v_1439, (V)"x") + R.Get(v_1444, (V)1.0d))}, new Results((V)(R.Get(v_1439, (V)"y") + R.Get(v_1444, (V)2.0d))))).First))) {
var values_1445 = R.Join(new V[] {}, new Results((V)true));
v_1441 = values_1445[0];
}
}
if (R.Truth((V)(!R.Truth(v_1441)))) {
R.Call(v_1118, R.Join(new V[] {(V)"door_unreachable", R.Get(v_1439, (V)"x"), R.Get(v_1439, (V)"y")}, new Results(R.Get(v_1439, (V)"id"))));
}
}
var values_1446 = R.Join(new V[] {}, new Results(R.Table()));
V v_1447 = values_1446[0];
foreach (var row_1448 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"secrets"), () => R.Table()))))))) {
V v_1449 = row_1448[0];
V v_1450 = row_1448[1];
var values_1451 = R.Join(new V[] {}, new Results(R.Get(v_1137, R.Get(v_1450, (V)"room_id"))));
V v_1452 = values_1451[0];
if (R.Truth(R.Or(R.Or(R.Get(v_1358, R.Get(v_1450, (V)"id")), () => (V)(!R.Truth(v_1452))), () => R.Get(v_1447, R.Get(v_1450, (V)"definition_id"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_invalid", R.Get(v_1450, (V)"x"), R.Get(v_1450, (V)"y")}, new Results(R.Get(v_1450, (V)"id"))));
}
var values_1453 = R.Join(new V[] {}, new Results((V)true));
var obj_1454 = v_1358;
V key_1455 = R.Get(v_1450, (V)"id");
R.Set(obj_1454, key_1455, values_1453[0]);
var values_1456 = R.Join(new V[] {}, new Results((V)true));
var obj_1457 = v_1447;
V key_1458 = R.Get(v_1450, (V)"definition_id");
R.Set(obj_1457, key_1458, values_1456[0]);
if (R.Truth((V)(!R.Truth(R.Get(v_1382, R.Call(v_65, R.Join(new V[] {R.Get(v_1450, (V)"x")}, new Results(R.Get(v_1450, (V)"y")))).First))))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_approach_unreachable", R.Get(v_1450, (V)"x"), R.Get(v_1450, (V)"y")}, new Results(R.Get(v_1450, (V)"id"))));
}
var values_1459 = R.Join(new V[] {}, new Results((V)false));
V v_1460 = values_1459[0];
foreach (var row_1461 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.And(v_1452, () => R.Get(R.Get(v_1452, (V)"markers"), (V)"secret_anchor")), () => R.Table()))))))) {
V v_1462 = row_1461[0];
V v_1463 = row_1461[1];
if (R.Truth(R.And((V)(R.Get(v_1463, (V)"x") == R.Get(v_1450, (V)"x")), () => (V)(R.Get(v_1463, (V)"y") == R.Get(v_1450, (V)"y"))))) {
var values_1464 = R.Join(new V[] {}, new Results((V)true));
v_1460 = values_1464[0];
}
}
if (R.Truth((V)(!R.Truth(v_1460)))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_anchor_invalid", R.Get(v_1450, (V)"x"), R.Get(v_1450, (V)"y")}, new Results(R.Get(v_1450, (V)"id"))));
}
if (R.Truth(R.Get(v_1450, (V)"wall_x"))) {
var values_1465 = R.Join(new V[] {}, new Results((V)false));
V v_1466 = values_1465[0];
foreach (var row_1467 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1452, (V)"markers"), (V)"secret_wall"), () => R.Table()))))))) {
V v_1468 = row_1467[0];
V v_1469 = row_1467[1];
if (R.Truth(R.And((V)(R.Get(v_1469, (V)"x") == R.Get(v_1450, (V)"wall_x")), () => (V)(R.Get(v_1469, (V)"y") == R.Get(v_1450, (V)"wall_y"))))) {
var values_1470 = R.Join(new V[] {}, new Results((V)true));
v_1466 = values_1470[0];
}
}
if (R.Truth(R.Or(R.Or(R.Or((V)(!R.Truth(v_1466)), () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_1450, (V)"x") - R.Get(v_1450, (V)"wall_x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(v_1450, (V)"y") - R.Get(v_1450, (V)"wall_y"))))).First) != (V)1.0d)), () => (V)(!R.Truth(R.Get(v_1197, R.Call(v_65, R.Join(new V[] {R.Get(v_1450, (V)"wall_x")}, new Results(R.Get(v_1450, (V)"wall_y")))).First)))), () => R.Get(v_1244, R.Call(v_65, R.Join(new V[] {R.Get(v_1450, (V)"cache_x")}, new Results(R.Get(v_1450, (V)"cache_y")))).First)))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_wall_invalid", R.Get(v_1450, (V)"wall_x"), R.Get(v_1450, (V)"wall_y")}, new Results(R.Get(v_1450, (V)"id"))));
}
var values_1471 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1181, R.Get(v_1450, (V)"id")), () => R.Get(R.Get(v_1181, R.Get(v_1450, (V)"id")), (V)"revealed"))));
V v_1472 = values_1471[0];
var values_1473 = R.Join(new V[] {}, new Results(R.Or(R.And(v_1472, () => (V)"."), () => (V)"#")));
V v_1474 = values_1473[0];
if (R.Truth(R.Or((V)(R.Invoke(R.Get(R.Get(v_1114, (V)"rows"), R.Get(v_1450, (V)"wall_y")), "sub", R.Join(new V[] {R.Get(v_1450, (V)"wall_x")}, new Results(R.Get(v_1450, (V)"wall_x")))).First != v_1474), () => (V)(R.Invoke(R.Get(R.Get(v_1114, (V)"rows"), R.Get(v_1450, (V)"cache_y")), "sub", R.Join(new V[] {R.Get(v_1450, (V)"cache_x")}, new Results(R.Get(v_1450, (V)"cache_x")))).First != v_1474)))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_geometry_invalid", R.Get(v_1450, (V)"wall_x"), R.Get(v_1450, (V)"wall_y")}, new Results(R.Get(v_1450, (V)"id"))));
}
if (R.Truth(R.And((V)(!R.Truth(v_1472)), () => (V)(R.Invoke(R.Get(R.Get(v_1114, (V)"visual_rows"), R.Get(v_1450, (V)"wall_y")), "sub", R.Join(new V[] {R.Get(v_1450, (V)"wall_x")}, new Results(R.Get(v_1450, (V)"wall_x")))).First != (V)"k")))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_clue_invalid", R.Get(v_1450, (V)"wall_x"), R.Get(v_1450, (V)"wall_y")}, new Results(R.Get(v_1450, (V)"id"))));
}
} else {
var values_1475 = R.Join(new V[] {}, new Results(R.And(R.Get(v_1181, R.Get(v_1450, (V)"id")), () => R.Get(R.Get(v_1181, R.Get(v_1450, (V)"id")), (V)"revealed"))));
V v_1476 = values_1475[0];
if (R.Truth((V)(R.Invoke(R.Get(R.Get(v_1114, (V)"visual_rows"), R.Get(v_1450, (V)"y")), "sub", R.Join(new V[] {R.Get(v_1450, (V)"x")}, new Results(R.Get(v_1450, (V)"x")))).First != R.Or(R.And(v_1476, () => (V)"m"), () => (V)"p")))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_clue_invalid", R.Get(v_1450, (V)"x"), R.Get(v_1450, (V)"y")}, new Results(R.Get(v_1450, (V)"id"))));
}
}
}
var values_1477 = R.Join(new V[] {}, new Results(R.Or(R.And(v_1133, () => (V)1.0d), () => (V)2.0d)));
V v_1478 = values_1477[0];
if (R.Truth(R.Or(R.Or((V)((V)R.Length(R.Or(R.Get(R.Get(v_1114, (V)"placements"), (V)"secrets"), () => R.Table())) != v_1478), () => R.And(R.Or((V)(!R.Truth(v_1133)), () => (V)(R.Get(v_1133, (V)"secret_kind") == (V)"cracked")), () => (V)(!R.Truth(R.Get(v_1447, (V)"core:secret/cracked_cache"))))), () => R.And(R.Or((V)(!R.Truth(v_1133)), () => (V)(R.Get(v_1133, (V)"secret_kind") == (V)"sigil")), () => (V)(!R.Truth(R.Get(v_1447, (V)"core:secret/moss_sigil"))))))) {
R.Call(v_1118, R.Join(new V[] {(V)"secret_count", (V)0.0d, (V)0.0d}, new Results((V)"wrong optional discovery mechanisms")));
}
var values_1479 = R.Join(new V[] {}, new Results(R.Get(v_1114, (V)"visual_rows")));
V v_1480 = values_1479[0];
if (R.Truth(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1480))).First != (V)"table"), () => (V)((V)R.Length(v_1480) != R.Get(v_1114, (V)"height"))))) {
R.Call(v_1118, R.Join(new V[] {(V)"visual_invalid", (V)0.0d, (V)0.0d}, new Results((V)"missing visual layer")));
} else {
foreach (var row_1481 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1480)))))) {
V v_1482 = row_1481[0];
V v_1483 = row_1481[1];
if (R.Truth(R.Or((V)((V)R.Length(v_1483) != R.Get(v_1114, (V)"width")), () => R.Invoke(v_1483, "find", R.Join(new V[] {}, new Results((V)"[^wsmcavrkpFfWbTtqIiLlOo]"))).First))) {
R.Call(v_1118, R.Join(new V[] {(V)"visual_invalid", (V)0.0d, v_1482}, new Results((V)"invalid decoration layer")));
}
}
}
return R.Join(new V[] {(V)((V)R.Length(v_1117) == (V)0.0d)}, new Results(v_1117));
return Results.Empty;
})));
R.Set(v_18, (V)"repair", new V((RuleFunction)(args_1484 => {
V v_1485 = args_1484[0];
V v_1486 = args_1484[1];
var values_1487 = R.Join(new V[] {}, new Results(R.Or(v_1486, () => (V)8.0d)));
v_1486 = values_1487[0];
var values_1488 = R.Join(new V[] {}, new Results(R.Table()));
V v_1489 = values_1488[0];
var values_1490 = R.Join(new V[] {}, new Results(R.Table()));
V v_1491 = values_1490[0];
foreach (var row_1492 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1485, (V)"editable_cells"), () => R.Table()))))))) {
V v_1493 = row_1492[0];
V v_1494 = row_1492[1];
var values_1495 = R.Join(new V[] {}, new Results((V)true));
var obj_1496 = v_1489;
V key_1497 = R.Call(v_65, R.Join(new V[] {R.Get(v_1494, (V)"x")}, new Results(R.Get(v_1494, (V)"y")))).First;
R.Set(obj_1496, key_1497, values_1495[0]);
}
foreach (var row_1498 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1485, (V)"protected_cells"), () => R.Table()))))))) {
V v_1499 = row_1498[0];
V v_1500 = row_1498[1];
var values_1501 = R.Join(new V[] {}, new Results((V)true));
var obj_1502 = v_1491;
V key_1503 = R.Call(v_65, R.Join(new V[] {R.Get(v_1500, (V)"x")}, new Results(R.Get(v_1500, (V)"y")))).First;
R.Set(obj_1502, key_1503, values_1501[0]);
}
var values_1504 = R.Join(new V[] {}, new Results(R.Table()));
V v_1505 = values_1504[0];
var values_1506 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"validate"), R.Join(new V[] {}, new Results(v_1485))));
V v_1507 = values_1506[0];
V v_1508 = values_1506[1];
{ double end_1511 = R.Num(v_1486), step_1512 = R.Num((V)1.0d);
for (double i_1510 = R.Num((V)1.0d); step_1512 > 0 ? i_1510 <= end_1511 : i_1510 >= end_1511; i_1510 += step_1512) {
V v_1509 = i_1510;
if (R.Truth(v_1507)) {
break;
}
var values_1513 = Results.Empty;
V v_1514 = values_1513[0];
foreach (var row_1515 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1508)))))) {
V v_1516 = row_1515[0];
V v_1517 = row_1515[1];
var values_1518 = R.Join(new V[] {}, R.Call(v_65, R.Join(new V[] {R.Get(v_1517, (V)"x")}, new Results(R.Get(v_1517, (V)"y")))));
V v_1519 = values_1518[0];
if (R.Truth(R.Or(R.And(R.And((V)(R.Get(v_1517, (V)"kind") == (V)"corridor_blocked"), () => R.Get(v_1489, v_1519)), () => (V)(!R.Truth(R.Get(v_1491, v_1519)))), () => (V)(R.Get(v_1517, (V)"kind") == (V)"secret_clue_invalid")))) {
var values_1520 = R.Join(new V[] {}, new Results(v_1517));
v_1514 = values_1520[0];

break;
}
}
if (R.Truth((V)(!R.Truth(v_1514)))) {
break;
}
if (R.Truth((V)(R.Get(v_1514, (V)"kind") == (V)"corridor_blocked"))) {
var values_1521 = R.Join(new V[] {}, R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1485, (V)"biome_id"), () => R.Get(R.Get(v_8, (V)"order"), (V)1.0d))))));
V v_1522 = values_1521[0];
R.Call(v_277, R.Join(new V[] {v_1485, R.Get(v_1514, (V)"x"), R.Get(v_1514, (V)"y"), (V)"."}, new Results(R.Get(v_1522, (V)"corridor_visual"))));
} else {
var values_1523 = Results.Empty;
V v_1524 = values_1523[0];
foreach (var row_1525 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_1485, (V)"placements"), (V)"secrets"))))))) {
V v_1526 = row_1525[0];
V v_1527 = row_1525[1];
if (R.Truth(R.Or(R.And((V)(R.Get(v_1527, (V)"wall_x") == R.Get(v_1514, (V)"x")), () => (V)(R.Get(v_1527, (V)"wall_y") == R.Get(v_1514, (V)"y"))), () => R.And(R.And((V)(!R.Truth(R.Get(v_1527, (V)"wall_x"))), () => (V)(R.Get(v_1527, (V)"x") == R.Get(v_1514, (V)"x"))), () => (V)(R.Get(v_1527, (V)"y") == R.Get(v_1514, (V)"y")))))) {
var values_1528 = R.Join(new V[] {}, new Results(v_1527));
v_1524 = values_1528[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_1524}, new Results((V)"missing secret clue repair target")));
var values_1529 = R.Join(new V[] {}, R.Invoke(R.Get(R.Get(v_1485, (V)"rows"), R.Get(v_1514, (V)"y")), "sub", R.Join(new V[] {R.Get(v_1514, (V)"x")}, new Results(R.Get(v_1514, (V)"x")))));
V v_1530 = values_1529[0];
R.Call(v_277, R.Join(new V[] {v_1485, R.Get(v_1514, (V)"x"), R.Get(v_1514, (V)"y"), v_1530}, new Results(R.Or(R.And(R.Get(v_1524, (V)"wall_x"), () => (V)"k"), () => (V)"p"))));
}
var values_1531 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"validate"), R.Join(new V[] {}, new Results(v_1485))));
v_1507 = values_1531[0];
v_1508 = values_1531[1];
var values_1532 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"kind", R.Or(R.And((V)(R.Get(v_1514, (V)"kind") == (V)"corridor_blocked"), () => (V)"restore_corridor"), () => (V)"restore_secret_clue")), new Entry((V)"x", R.Get(v_1514, (V)"x")), new Entry((V)"y", R.Get(v_1514, (V)"y")), new Entry((V)"valid_after", v_1507))));
var obj_1533 = v_1505;
V key_1534 = (V)((V)R.Length(v_1505) + (V)1.0d);
R.Set(obj_1533, key_1534, values_1532[0]);
}
}
return R.Join(new V[] {v_1507, v_1508}, new Results(v_1505));
return Results.Empty;
})));
V v_1535 = V.Nil;
v_1535 = new V((RuleFunction)(args_1536 => {
V v_1537 = args_1536[0];
V v_1538 = args_1536[1];
if (R.Truth((V)(v_1538 == (V)"editable"))) {
foreach (var row_1539 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_1537, (V)"corridors"))))))) {
V v_1540 = row_1539[0];
V v_1541 = row_1539[1];
{ double end_1544 = R.Num((V)((V)R.Length(R.Get(v_1541, (V)"cells")) - (V)1.0d)), step_1545 = R.Num((V)1.0d);
for (double i_1543 = R.Num((V)2.0d); step_1545 > 0 ? i_1543 <= end_1544 : i_1543 >= end_1544; i_1543 += step_1545) {
V v_1542 = i_1543;
var values_1546 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1541, (V)"cells"), v_1542)));
V v_1547 = values_1546[0];
R.Call(v_277, R.Join(new V[] {v_1537, R.Get(v_1547, (V)"x"), R.Get(v_1547, (V)"y"), (V)"#"}, new Results((V)"w")));
return R.Join(new V[] {}, new Results(v_1547));
}
}
}
} else if (R.Truth((V)(v_1538 == (V)"protected"))) {
var values_1548 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_1537, (V)"corridors"), (V)1.0d), (V)"from_socket")));
V v_1549 = values_1548[0];
R.Call(v_277, R.Join(new V[] {v_1537, R.Get(v_1549, (V)"x"), R.Get(v_1549, (V)"y"), (V)"#"}, new Results((V)"w")));
return R.Join(new V[] {}, new Results(v_1549));
} else if (R.Truth((V)(v_1538 == (V)"secret_clue"))) {
var values_1550 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_1537, (V)"placements"), (V)"secrets"), (V)1.0d)));
V v_1551 = values_1550[0];
var values_1552 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_1537, (V)"rows"), R.Get(v_1551, (V)"wall_y"))));
V v_1553 = values_1552[0];
R.Call(v_277, R.Join(new V[] {v_1537, R.Get(v_1551, (V)"wall_x"), R.Get(v_1551, (V)"wall_y"), R.Invoke(v_1553, "sub", R.Join(new V[] {R.Get(v_1551, (V)"wall_x")}, new Results(R.Get(v_1551, (V)"wall_x")))).First}, new Results((V)"w")));
return R.Join(new V[] {}, new Results(v_1551));
}
return Results.Empty;
}));
R.Set(v_18, (V)"generate", new V((RuleFunction)(args_1554 => {
V v_1555 = args_1554[0];
V v_1556 = args_1554[1];
var values_1557 = R.Join(new V[] {}, new Results(R.Or(v_1556, () => R.Table())));
v_1556 = values_1557[0];
var values_1558 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1556, (V)"studio_pack"), () => v_44)));
V v_1559 = values_1558[0];
if (R.Truth(v_1559)) {
R.Call(R.Get(v_12, (V)"validate"), R.Join(new V[] {}, new Results(v_1559)));
}
var values_1560 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1556, (V)"biome_id"), () => R.Get(R.Get(v_8, (V)"order"), (V)1.0d))))).First}, new Results((V)"unknown generation biome"))));
V v_1561 = values_1560[0];
R.Call(R.Get(v_8, (V)"validate"), Results.Empty);
var values_1562 = R.Join(new V[] {}, new Results(R.Table()));
V v_1563 = values_1562[0];
foreach (var row_1564 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_6, (V)"order"))))))) {
V v_1565 = row_1564[0];
V v_1566 = row_1564[1];
var values_1567 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_1561, (V)"room_prefix"), R.Invoke(v_1566, "match", R.Join(new V[] {}, new Results((V)"[^/]+$"))).First)));
var obj_1568 = v_1563;
V key_1569 = (V)((V)R.Length(v_1563) + (V)1.0d);
R.Set(obj_1568, key_1569, values_1567[0]);
}
var values_1570 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", R.Get(v_1561, (V)"id")), new Entry((V)"floor_id", R.Get(v_1561, (V)"floor_id")), new Entry((V)"width", R.Get(v_18, (V)"width")), new Entry((V)"height", R.Get(v_18, (V)"height")), new Entry((V)"room_min", R.Get(v_18, (V)"room_min")), new Entry((V)"room_max", R.Get(v_18, (V)"room_max")), new Entry((V)"standard_chests", R.Get(v_18, (V)"chest_target")), new Entry((V)"retry_limit", R.Get(R.Get(v_6, (V)"biome"), (V)"retry_limit")), new Entry((V)"enemy_ids", R.Get(v_1561, (V)"enemy_ids")))));
V v_1571 = values_1570[0];
R.Call(R.Get(v_6, (V)"validate"), R.Join(new V[] {R.Get(v_6, (V)"definitions"), v_1563}, new Results(v_1571)));
R.Call(R.Get(v_10, (V)"validate"), Results.Empty);
R.Call(R.Get(v_4, (V)"new"), R.Join(new V[] {}, new Results(v_1555)));
var values_1572 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1556, (V)"retry_limit"), () => R.Get(R.Get(v_6, (V)"biome"), (V)"retry_limit"))));
V v_1573 = values_1572[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1573))).First == (V)"number"), () => (V)((V)(v_1573 % (V)1.0d) == (V)0.0d)), () => (V)(v_1573 >= (V)0.0d)), () => (V)(v_1573 <= (V)8.0d))}, new Results((V)"invalid generation retry budget")));
var values_1574 = R.Join(new V[] {}, new Results(R.Table()));
V v_1575 = values_1574[0];
{ double end_1578 = R.Num(v_1573), step_1579 = R.Num((V)1.0d);
for (double i_1577 = R.Num((V)0.0d); step_1579 > 0 ? i_1577 <= end_1578 : i_1577 >= end_1578; i_1577 += step_1579) {
V v_1576 = i_1577;
var values_1580 = R.Join(new V[] {}, new Results(R.Concat(R.Get(v_1561, (V)"floor_id"), R.Concat((V)":", R.Concat(R.Or(R.And(R.Get(v_1556, (V)"floor_index"), () => R.Concat((V)"depth:", R.Concat(R.Get(v_1556, (V)"floor_index"), (V)":"))), () => (V)""), v_1576)))));
V v_1581 = values_1580[0];
var values_1582 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {v_1555}, new Results(v_1581))));
V v_1583 = values_1582[0];
var values_1584 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_774, v_1555, v_1576, v_1583, v_1561, v_1559}, new Results(v_1556))));
V v_1585 = values_1584[0];
V v_1586 = values_1584[1];
var values_1587 = R.Join(new V[] {}, new Results(R.Or(R.And(v_1585, () => v_1586), () => V.Nil)));
V v_1588 = values_1587[0];
if (R.Truth(v_1588)) {
if (R.Truth(R.Get(v_1556, (V)"fault"))) {
R.Call(v_1535, R.Join(new V[] {v_1588}, new Results(R.Get(v_1556, (V)"fault"))));
}
var values_1589 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"repair"), R.Join(new V[] {v_1588}, new Results((V)8.0d))));
V v_1590 = values_1589[0];
V v_1591 = values_1589[1];
V v_1592 = values_1589[2];
var values_1593 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"attempt", v_1576), new Entry((V)"derived_seed", v_1583), new Entry((V)"valid", v_1590), new Entry((V)"error_count", (V)R.Length(v_1591)), new Entry((V)"first_error", R.Get(v_1591, (V)1.0d)), new Entry((V)"repairs", v_1592))));
var obj_1594 = v_1575;
V key_1595 = (V)((V)R.Length(v_1575) + (V)1.0d);
R.Set(obj_1594, key_1595, values_1593[0]);
if (R.Truth(v_1590)) {
var values_1596 = R.Join(new V[] {}, new Results(v_1592));
var obj_1597 = v_1588;
V key_1598 = (V)"repairs";
R.Set(obj_1597, key_1598, values_1596[0]);
var values_1599 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"attempts", v_1575), new Entry((V)"fallback", (V)false))));
var obj_1600 = v_1588;
V key_1601 = (V)"diagnostics";
R.Set(obj_1600, key_1601, values_1599[0]);
return R.Join(new V[] {}, new Results(v_1588));
}
} else {
var values_1602 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"attempt", v_1576), new Entry((V)"derived_seed", v_1583), new Entry((V)"valid", (V)false), new Entry((V)"error_count", (V)1.0d), new Entry((V)"error", R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_1586))).First), new Entry((V)"repairs", R.Table()))));
var obj_1603 = v_1575;
V key_1604 = (V)((V)R.Length(v_1575) + (V)1.0d);
R.Set(obj_1603, key_1604, values_1602[0]);
}
}
}
var values_1605 = R.Join(new V[] {}, R.Call(R.Get(v_4, (V)"derive"), R.Join(new V[] {(V)314159.0d}, new Results(R.Concat(R.Get(v_1561, (V)"floor_id"), R.Concat((V)":", R.Concat(R.Or(R.And(R.Get(v_1556, (V)"floor_index"), () => R.Concat((V)"depth:", R.Concat(R.Get(v_1556, (V)"floor_index"), (V)":"))), () => (V)""), (V)"fallback")))))));
V v_1606 = values_1605[0];
var values_1607 = R.Join(new V[] {}, R.Call(rt.Global("pcall"), R.Join(new V[] {v_774, v_1555, (-(V)1.0d), v_1606, v_1561, v_1559}, new Results(v_1556))));
V v_1608 = values_1607[0];
V v_1609 = values_1607[1];
var values_1610 = R.Join(new V[] {(V)false}, new Results(R.Table()));
V v_1611 = values_1610[0];
V v_1612 = values_1610[1];
if (R.Truth(v_1608)) {
var values_1613 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"validate"), R.Join(new V[] {}, new Results(v_1609))));
v_1611 = values_1613[0];
v_1612 = values_1613[1];
}
var values_1614 = R.Join(new V[] {}, new Results((V)false));
V v_1615 = values_1614[0];
if (R.Truth(R.And((V)(!R.Truth(v_1611)), () => v_1559))) {
var values_1616 = R.Join(new V[] {}, R.Call(v_774, R.Join(new V[] {v_1555, (-(V)1.0d), v_1606, v_1561, V.Nil}, new Results(v_1556))));
v_1609 = values_1616[0];
var values_1617 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"validate"), R.Join(new V[] {}, new Results(v_1609))));
v_1611 = values_1617[0];
v_1612 = values_1617[1];
var values_1618 = R.Join(new V[] {}, new Results((V)true));
v_1615 = values_1618[0];
var values_1619 = R.Join(new V[] {}, R.Call(v_45, R.Join(new V[] {}, new Results(v_1559))));
var obj_1620 = v_1609;
V key_1621 = (V)"studio_pack_fingerprint";
R.Set(obj_1620, key_1621, values_1619[0]);
var values_1622 = R.Join(new V[] {}, new Results(R.Table()));
var obj_1623 = v_1609;
V key_1624 = (V)"studio_templates";
R.Set(obj_1623, key_1624, values_1622[0]);
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_1611}, new Results(R.Concat((V)"prevalidated fallback failed: ", R.Or(R.And(R.Get(v_1612, (V)1.0d), () => R.Get(R.Get(v_1612, (V)1.0d), (V)"kind")), () => R.Or(R.And((V)(!R.Truth(v_1608)), () => R.Call(rt.Global("tostring"), R.Join(new V[] {}, new Results(v_1609))).First), () => (V)"unknown"))))));
var values_1625 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"attempts", v_1575), new Entry((V)"fallback", (V)true), new Entry((V)"fallback_reference_seed", v_1606), new Entry((V)"authored_excluded", v_1615))));
var obj_1626 = v_1609;
V key_1627 = (V)"diagnostics";
R.Set(obj_1626, key_1627, values_1625[0]);
return R.Join(new V[] {}, new Results(v_1609));
return Results.Empty;
})));
R.Set(v_18, (V)"generate_pair", new V((RuleFunction)(args_1628 => {
V v_1629 = args_1628[0];
V v_1630 = args_1628[1];
var values_1631 = R.Join(new V[] {}, new Results(R.Or(v_1630, () => R.Table())));
v_1630 = values_1631[0];
var values_1632 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_8, (V)"get"), R.Join(new V[] {}, new Results(R.Or(R.Get(v_1630, (V)"biome_id"), () => R.Get(R.Get(v_8, (V)"order"), (V)1.0d))))).First}, new Results((V)"unknown connected-floor biome"))));
V v_1633 = values_1632[0];
var values_1634 = R.Join(new V[] {}, new Results(R.Or(R.Get(v_1630, (V)"stage_index"), () => (V)1.0d)));
V v_1635 = values_1634[0];
R.Call(rt.Global("assert"), R.Join(new V[] {R.And(R.And(R.And((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1635))).First == (V)"number"), () => (V)((V)(v_1635 % (V)1.0d) == (V)0.0d)), () => (V)(v_1635 >= (V)1.0d)), () => (V)(v_1635 <= (V)3.0d))}, new Results((V)"invalid connected-floor stage")));
var values_1636 = R.Join(new V[] {}, new Results(R.Table()));
V v_1637 = values_1636[0];
{ double end_1640 = R.Num((V)2.0d), step_1641 = R.Num((V)1.0d);
for (double i_1639 = R.Num((V)1.0d); step_1641 > 0 ? i_1639 <= end_1640 : i_1639 >= end_1640; i_1639 += step_1641) {
V v_1638 = i_1639;
var values_1642 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"grid_cols", (V)3.0d), new Entry((V)"grid_rows", (V)2.0d), new Entry((V)"room_min", (V)6.0d), new Entry((V)"room_max", (V)6.0d), new Entry((V)"terminal_id", (V)6.0d), new Entry((V)"width", (V)104.0d), new Entry((V)"height", (V)68.0d), new Entry((V)"base_x", (V)20.0d), new Entry((V)"base_y", (V)16.0d), new Entry((V)"slot_x", (V)32.0d), new Entry((V)"slot_y", (V)26.0d), new Entry((V)"chest_target", R.Or(R.And((V)(v_1638 == (V)1.0d), () => (V)2.0d), () => (V)3.0d)), new Entry((V)"secret_kind", R.Or(R.And((V)(v_1638 == (V)1.0d), () => (V)"cracked"), () => (V)"sigil")))));
V v_1643 = values_1642[0];
var values_1644 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"generate"), R.Join(new V[] {v_1629}, new Results(R.Table(new Entry((V)"biome_id", R.Get(v_1633, (V)"id")), new Entry((V)"studio_pack", R.Get(v_1630, (V)"studio_pack")), new Entry((V)"retry_limit", R.Get(v_1630, (V)"retry_limit")), new Entry((V)"floor_index", v_1638), new Entry((V)"layout", v_1643), new Entry((V)"floor_id", R.Concat(R.Get(v_1633, (V)"floor_id"), R.Concat((V)"/stage/", R.Concat(v_1635, R.Concat((V)"/depth/", v_1638))))))))));
var obj_1645 = v_1637;
V key_1646 = v_1638;
R.Set(obj_1645, key_1646, values_1644[0]);
}
}
var values_1647 = R.Join(new V[] {R.Get(v_1637, (V)1.0d)}, new Results(R.Get(v_1637, (V)2.0d)));
V v_1648 = values_1647[0];
V v_1649 = values_1647[1];
var values_1650 = R.Join(new V[] {R.Or(R.And(R.Get(v_1648, (V)"stair_anchor"), () => R.Get(R.Get(v_1648, (V)"stair_anchor"), (V)"x")), () => R.Get(R.Get(v_1648, (V)"elite"), (V)"x"))}, new Results(R.Or(R.And(R.Get(v_1648, (V)"stair_anchor"), () => R.Get(R.Get(v_1648, (V)"stair_anchor"), (V)"y")), () => R.Get(R.Get(v_1648, (V)"elite"), (V)"y"))));
V v_1651 = values_1650[0];
V v_1652 = values_1650[1];
var values_1653 = R.Join(new V[] {R.Or(R.And(R.Get(v_1649, (V)"stair_anchor"), () => R.Get(R.Get(v_1649, (V)"stair_anchor"), (V)"x")), () => R.Get(v_1649, (V)"start_x"))}, new Results(R.Or(R.And(R.Get(v_1649, (V)"stair_anchor"), () => R.Get(R.Get(v_1649, (V)"stair_anchor"), (V)"y")), () => R.Get(v_1649, (V)"start_y"))));
V v_1654 = values_1653[0];
V v_1655 = values_1653[1];
var values_1656 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"stair:down"), new Entry((V)"x", v_1651), new Entry((V)"y", v_1652), new Entry((V)"to_floor_id", R.Get(v_1649, (V)"floor_id")), new Entry((V)"to_x", v_1654), new Entry((V)"to_y", v_1655))))));
var obj_1657 = R.Get(v_1648, (V)"placements");
V key_1658 = (V)"stairs";
R.Set(obj_1657, key_1658, values_1656[0]);
var values_1659 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"id", (V)"stair:up"), new Entry((V)"x", v_1654), new Entry((V)"y", v_1655), new Entry((V)"to_floor_id", R.Get(v_1648, (V)"floor_id")), new Entry((V)"to_x", v_1651), new Entry((V)"to_y", v_1652))))));
var obj_1660 = R.Get(v_1649, (V)"placements");
V key_1661 = (V)"stairs";
R.Set(obj_1660, key_1661, values_1659[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(R.Get(v_18, (V)"validate_pair"), R.Join(new V[] {}, new Results(v_1637))).First}, new Results((V)"invalid connected biome floors")));
return R.Join(new V[] {}, new Results(v_1637));
return Results.Empty;
})));
R.Set(v_18, (V)"validate_pair", new V((RuleFunction)(args_1662 => {
V v_1663 = args_1662[0];
V v_1664 = args_1662[1];
if (R.Truth(R.Or((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_1663))).First != (V)"table"), () => (V)((V)R.Length(v_1663) != (V)2.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"expected two floors"));
}
var values_1665 = R.Join(new V[] {R.Get(v_1663, (V)1.0d)}, new Results(R.Get(v_1663, (V)2.0d)));
V v_1666 = values_1665[0];
V v_1667 = values_1665[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_1666, (V)"floor_id") == R.Get(v_1667, (V)"floor_id")), () => (V)(R.Get(v_1666, (V)"biome_id") != R.Get(v_1667, (V)"biome_id"))), () => (V)((V)((V)R.Length(R.Get(v_1666, (V)"rooms")) + (V)R.Length(R.Get(v_1667, (V)"rooms"))) < (V)8.0d)), () => (V)((V)((V)R.Length(R.Get(v_1666, (V)"rooms")) + (V)R.Length(R.Get(v_1667, (V)"rooms"))) > (V)12.0d)), () => (V)((V)((V)R.Length(R.Get(R.Get(v_1666, (V)"placements"), (V)"chests")) + (V)R.Length(R.Get(R.Get(v_1667, (V)"placements"), (V)"chests"))) != (V)5.0d)), () => (V)((V)((V)R.Length(R.Get(R.Get(v_1666, (V)"placements"), (V)"secrets")) + (V)R.Length(R.Get(R.Get(v_1667, (V)"placements"), (V)"secrets"))) != (V)2.0d)))) {
return R.Join(new V[] {(V)false}, new Results((V)"invalid biome-wide budget"));
}
foreach (var row_1668 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_1663)))))) {
V v_1669 = row_1668[0];
V v_1670 = row_1668[1];
var values_1671 = R.Join(new V[] {}, R.Call(R.Get(v_18, (V)"validate"), R.Join(new V[] {v_1670}, new Results(v_1664))));
V v_1672 = values_1671[0];
V v_1673 = values_1671[1];
if (R.Truth((V)(!R.Truth(v_1672)))) {
return R.Join(new V[] {(V)false}, new Results(R.Or(R.And(R.Get(v_1673, (V)1.0d), () => R.Get(R.Get(v_1673, (V)1.0d), (V)"kind")), () => (V)"floor invalid")));
}
var values_1674 = R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_1670, (V)"placements"), (V)"stairs"), () => R.Get(R.Get(R.Get(v_1670, (V)"placements"), (V)"stairs"), (V)1.0d))));
V v_1675 = values_1674[0];
if (R.Truth(R.Or(R.Or((V)(!R.Truth(v_1675)), () => (V)((V)R.Length(R.Get(R.Get(v_1670, (V)"placements"), (V)"stairs")) != (V)1.0d)), () => (V)(!R.Truth(R.Call(R.Get(v_2, (V)"walkable"), R.Join(new V[] {v_1670, R.Get(v_1675, (V)"x")}, new Results(R.Get(v_1675, (V)"y")))).First))))) {
return R.Join(new V[] {(V)false}, new Results((V)"missing walkable stair"));
}
foreach (var row_1676 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"enemies"), new Entry((V)2, (V)"chests"), new Entry((V)3, (V)"doors")))))))) {
V v_1677 = row_1676[0];
V v_1678 = row_1676[1];
foreach (var row_1679 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Or(R.Get(R.Get(v_1670, (V)"placements"), v_1678), () => R.Table()))))))) {
V v_1680 = row_1679[0];
V v_1681 = row_1679[1];
if (R.Truth(R.And((V)(R.Get(v_1681, (V)"x") == R.Get(v_1675, (V)"x")), () => (V)(R.Get(v_1681, (V)"y") == R.Get(v_1675, (V)"y"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"stair overlaps placement"));
}
}
}
}
var values_1682 = R.Join(new V[] {R.Get(R.Get(R.Get(v_1666, (V)"placements"), (V)"stairs"), (V)1.0d)}, new Results(R.Get(R.Get(R.Get(v_1667, (V)"placements"), (V)"stairs"), (V)1.0d)));
V v_1683 = values_1682[0];
V v_1684 = values_1682[1];
if (R.Truth(R.Or(R.Or(R.Or(R.Or(R.Or((V)(R.Get(v_1683, (V)"to_floor_id") != R.Get(v_1667, (V)"floor_id")), () => (V)(R.Get(v_1683, (V)"to_x") != R.Get(v_1684, (V)"x"))), () => (V)(R.Get(v_1683, (V)"to_y") != R.Get(v_1684, (V)"y"))), () => (V)(R.Get(v_1684, (V)"to_floor_id") != R.Get(v_1666, (V)"floor_id"))), () => (V)(R.Get(v_1684, (V)"to_x") != R.Get(v_1683, (V)"x"))), () => (V)(R.Get(v_1684, (V)"to_y") != R.Get(v_1683, (V)"y"))))) {
return R.Join(new V[] {(V)false}, new Results((V)"stair links are not reciprocal"));
}
return R.Join(new V[] {}, new Results((V)true));
return Results.Empty;
})));
return R.Join(new V[] {}, new Results(v_18));
return Results.Empty;
} }
