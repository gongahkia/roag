// Static C# port of tests/generation_tests.lua at d498fe0889050d2a23f2ff86d80db6084ed6467a.
// Original SHA256: 94a23ef89b1031d198b42d6e822182837e3a0795c0c2727246e15172893ef3a8
#nullable disable
#pragma warning disable CS0162, CS0219, CS1718
using Roag.Core.Rules;
namespace Roag.Core.Rules.Generated;
internal static class Rules_generation_tests { internal static Results Load(NativeRuntime rt) {
return R.Join(new V[] {}, new Results(new V((RuleFunction)(args_1 => {
V v_2 = args_1[0];
V v_3 = args_1[1];
var values_4 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.ai"))));
V v_5 = values_4[0];
var values_6 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.content"))));
V v_7 = values_6[0];
var values_8 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.game"))));
V v_9 = values_8[0];
var values_10 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.generation"))));
V v_11 = values_10[0];
var values_12 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rng"))));
V v_13 = values_12[0];
var values_14 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.rooms"))));
V v_15 = values_14[0];
var values_16 = R.Join(new V[] {}, R.Call(rt.Global("require"), R.Join(new V[] {}, new Results((V)"src.scheduler"))));
V v_17 = values_16[0];
V v_18 = V.Nil;
v_18 = new V((RuleFunction)(args_19 => {
V v_20 = args_19[0];
if (R.Truth((V)(R.Call(rt.Global("type"), R.Join(new V[] {}, new Results(v_20))).First != (V)"table"))) {
return R.Join(new V[] {}, new Results(v_20));
}
var values_21 = R.Join(new V[] {}, new Results(R.Table()));
V v_22 = values_21[0];
foreach (var row_23 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_20)))))) {
V v_24 = row_23[0];
V v_25 = row_23[1];
var values_26 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(v_25))));
var obj_27 = v_22;
V key_28 = v_24;
R.Set(obj_27, key_28, values_26[0]);
}
return R.Join(new V[] {}, new Results(v_22));
return Results.Empty;
}));
V v_29 = V.Nil;
v_29 = new V((RuleFunction)(args_30 => {
V v_31 = args_30[0];
V v_32 = args_30[1];
var values_33 = R.Join(new V[] {}, new Results(R.Table()));
V v_34 = values_33[0];
foreach (var row_35 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_31)))))) {
V v_36 = row_35[0];
V v_37 = row_35[1];
if (R.Truth((V)(R.Get(v_37, (V)"kind") == v_32))) {
var values_38 = R.Join(new V[] {}, new Results(v_37));
var obj_39 = v_34;
V key_40 = (V)((V)R.Length(v_34) + (V)1.0d);
R.Set(obj_39, key_40, values_38[0]);
}
}
return R.Join(new V[] {}, new Results(v_34));
return Results.Empty;
}));
var values_41 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (-(V)1.0d)))), new Entry((V)2, R.Table(new Entry((V)1, (V)1.0d), new Entry((V)2, (V)0.0d))), new Entry((V)3, R.Table(new Entry((V)1, (V)0.0d), new Entry((V)2, (V)1.0d))), new Entry((V)4, R.Table(new Entry((V)1, (-(V)1.0d)), new Entry((V)2, (V)0.0d))))));
V v_42 = values_41[0];
V v_43 = V.Nil;
v_43 = new V((RuleFunction)(args_44 => {
V v_45 = args_44[0];
V v_46 = args_44[1];
return R.Join(new V[] {}, new Results(R.Concat(v_45, R.Concat((V)",", v_46))));
return Results.Empty;
}));
V v_47 = V.Nil;
v_47 = new V((RuleFunction)(args_48 => {
V v_49 = args_48[0];
foreach (var row_50 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_49, (V)"entities"))))))) {
V v_51 = row_50[0];
V v_52 = row_50[1];
if (R.Truth((V)(R.Get(v_52, (V)"controller") == (V)"ai"))) {
R.Call(R.Get(v_17, (V)"remove_for_actor"), R.Join(new V[] {v_49}, new Results(R.Get(v_52, (V)"id"))));
}
}
return Results.Empty;
}));
V v_53 = V.Nil;
v_53 = new V((RuleFunction)(args_54 => {
V v_55 = args_54[0];
V v_56 = args_54[1];
var values_57 = R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"map_for"), R.Join(new V[] {}, new Results(v_55))));
V v_58 = values_57[0];
var values_59 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_55))));
V v_60 = values_59[0];
var values_61 = R.Join(new V[] {}, new Results(R.Table()));
V v_62 = values_61[0];
foreach (var row_63 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_55, (V)"entities"))))))) {
V v_64 = row_63[0];
V v_65 = row_63[1];
if (R.Truth(R.And((V)(R.Get(v_65, (V)"id") != R.Get(v_60, (V)"id")), () => R.Get(v_65, (V)"blocks_movement")))) {
var values_66 = R.Join(new V[] {}, new Results((V)true));
var obj_67 = v_62;
V key_68 = R.Call(v_43, R.Join(new V[] {R.Get(R.Get(v_65, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_65, (V)"position"), (V)"y")))).First;
R.Set(obj_67, key_68, values_66[0]);
}
}
var values_69 = R.Join(new V[] {}, new Results(R.Get(v_60, (V)"position")));
V v_70 = values_69[0];
var values_71 = R.Join(new V[] {}, new Results(R.Table(new Entry(R.Call(v_43, R.Join(new V[] {R.Get(v_70, (V)"x")}, new Results(R.Get(v_70, (V)"y")))).First, (V)true))));
V v_72 = values_71[0];
var values_73 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, R.Table(new Entry((V)"x", R.Get(v_70, (V)"x")), new Entry((V)"y", R.Get(v_70, (V)"y")), new Entry((V)"path", R.Table()))))));
V v_74 = values_73[0];
var values_75 = R.Join(new V[] {}, new Results((V)1.0d));
V v_76 = values_75[0];
while (R.Truth((V)(v_76 <= (V)R.Length(v_74)))) {
var values_77 = R.Join(new V[] {}, new Results(R.Get(v_74, v_76)));
V v_78 = values_77[0];

var values_79 = R.Join(new V[] {}, new Results((V)(v_76 + (V)1.0d)));
v_76 = values_79[0];
if (R.Truth(R.Call(v_56, R.Join(new V[] {R.Get(v_78, (V)"x")}, new Results(R.Get(v_78, (V)"y")))).First)) {
return R.Join(new V[] {}, new Results(R.Get(v_78, (V)"path")));
}
foreach (var row_80 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_42)))))) {
V v_81 = row_80[0];
V v_82 = row_80[1];
var values_83 = R.Join(new V[] {(V)(R.Get(v_78, (V)"x") + R.Get(v_82, (V)1.0d))}, new Results((V)(R.Get(v_78, (V)"y") + R.Get(v_82, (V)2.0d))));
V v_84 = values_83[0];
V v_85 = values_83[1];
var values_86 = R.Join(new V[] {}, R.Call(v_43, R.Join(new V[] {v_84}, new Results(v_85))));
V v_87 = values_86[0];
if (R.Truth(R.And(R.And((V)(!R.Truth(R.Get(v_72, v_87))), () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_58, v_84}, new Results(v_85))).First), () => (V)(!R.Truth(R.Get(v_62, v_87)))))) {
var values_88 = R.Join(new V[] {}, new Results((V)true));
var obj_89 = v_72;
V key_90 = v_87;
R.Set(obj_89, key_90, values_88[0]);
var values_91 = R.Join(new V[] {}, new Results(R.Table()));
V v_92 = values_91[0];
foreach (var row_93 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_78, (V)"path"))))))) {
V v_94 = row_93[0];
V v_95 = row_93[1];
var values_96 = R.Join(new V[] {}, new Results(v_95));
var obj_97 = v_92;
V key_98 = v_94;
R.Set(obj_97, key_98, values_96[0]);
}
var values_99 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"dx", R.Get(v_82, (V)1.0d)), new Entry((V)"dy", R.Get(v_82, (V)2.0d)))));
var obj_100 = v_92;
V key_101 = (V)((V)R.Length(v_92) + (V)1.0d);
R.Set(obj_100, key_101, values_99[0]);
var values_102 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_84), new Entry((V)"y", v_85), new Entry((V)"path", v_92))));
var obj_103 = v_74;
V key_104 = (V)((V)R.Length(v_74) + (V)1.0d);
R.Set(obj_103, key_104, values_102[0]);
}
}
}
return R.Join(new V[] {}, new Results(V.Nil));
return Results.Empty;
}));
V v_105 = V.Nil;
v_105 = new V((RuleFunction)(args_106 => {
V v_107 = args_106[0];
V v_108 = args_106[1];
var values_109 = R.Join(new V[] {}, R.Call(rt.Global("assert"), R.Join(new V[] {R.Call(v_53, R.Join(new V[] {v_107}, new Results(v_108))).First}, new Results((V)"generated route not reachable"))));
V v_110 = values_109[0];
foreach (var row_111 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_110)))))) {
V v_112 = row_111[0];
V v_113 = row_111[1];
var values_114 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_107}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", R.Get(v_113, (V)"dx")), new Entry((V)"dy", R.Get(v_113, (V)"dy")))))));
V v_115 = values_114[0];
V v_116 = values_114[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_115}, new Results(v_116)));
}
return R.Join(new V[] {}, new Results((V)R.Length(v_110)));
return Results.Empty;
}));
R.Call(v_2, R.Join(new V[] {(V)"room schema catches malformed sockets, masks and transforms"}, new Results(new V((RuleFunction)(args_117 => {

R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"validate"), Results.Empty)));
var values_118 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)"definitions")))));
V v_119 = values_118[0];
var values_120 = R.Join(new V[] {}, new Results((V)"E"));
var obj_121 = R.Get(R.Get(R.Get(v_119, (V)"core:room/entry"), (V)"sockets"), (V)1.0d);
V key_122 = (V)"facing";
R.Set(obj_121, key_122, values_120[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_15, (V)"validate"), v_119}, new Results(R.Get(v_15, (V)"order")))).First)))));
var values_123 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)"definitions")))));
v_119 = values_123[0];
var values_124 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", (V)5.0d), new Entry((V)"y", (V)4.0d))));
var obj_125 = R.Get(R.Get(v_119, (V)"core:room/entry"), (V)"editable");
V key_126 = (V)1.0d;
R.Set(obj_125, key_126, values_124[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_15, (V)"validate"), v_119}, new Results(R.Get(v_15, (V)"order")))).First)))));
var values_127 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)"definitions")))));
v_119 = values_127[0];
var values_128 = R.Join(new V[] {}, new Results((V)"#..x....#"));
var obj_129 = R.Get(R.Get(v_119, (V)"core:room/entry"), (V)"rows");
V key_130 = (V)2.0d;
R.Set(obj_129, key_130, values_128[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_15, (V)"validate"), v_119}, new Results(R.Get(v_15, (V)"order")))).First)))));
var values_131 = R.Join(new V[] {}, R.Call(v_18, R.Join(new V[] {}, new Results(R.Get(v_15, (V)"order")))));
V v_132 = values_131[0];

var values_133 = R.Join(new V[] {}, new Results(R.Get(v_132, (V)1.0d)));
var obj_134 = v_132;
V key_135 = (V)((V)R.Length(v_132) + (V)1.0d);
R.Set(obj_134, key_135, values_133[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_15, (V)"validate"), R.Get(v_15, (V)"definitions")}, new Results(v_132))).First)))));
var values_136 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_15, (V)"biome"), (V)"enemy_ids"), (V)1.0d)));
V v_137 = values_136[0];
var values_138 = R.Join(new V[] {}, new Results((V)"missing:enemy"));
var obj_139 = R.Get(R.Get(v_15, (V)"biome"), (V)"enemy_ids");
V key_140 = (V)1.0d;
R.Set(obj_139, key_140, values_138[0]);
var values_141 = R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {}, new Results(R.Get(v_15, (V)"validate")))).First))));
V v_142 = values_141[0];
var values_143 = R.Join(new V[] {}, new Results(v_137));
var obj_144 = R.Get(R.Get(v_15, (V)"biome"), (V)"enemy_ids");
V key_145 = (V)1.0d;
R.Set(obj_144, key_145, values_143[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_142)));
var values_146 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_15, (V)"definitions"), (V)"core:room/entry")));
V v_147 = values_146[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_15, (V)"transform"), v_147, (V)4.0d}, new Results((V)false))).First)))));
var values_148 = R.Join(new V[] {}, R.Call(R.Get(v_15, (V)"transform"), R.Join(new V[] {R.Get(R.Get(v_15, (V)"definitions"), (V)"core:room/gallery"), (V)1.0d}, new Results((V)true))));
V v_149 = values_148[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_149, (V)"width") == (V)11.0d), () => (V)(R.Get(v_149, (V)"height") == (V)15.0d)))));
var values_150 = Results.Empty;
V v_151 = values_150[0];
foreach (var row_152 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_149, (V)"sockets"))))))) {
V v_153 = row_152[0];
V v_154 = row_152[1];
if (R.Truth((V)(R.Get(v_154, (V)"facing") == (V)"E"))) {
var values_155 = R.Join(new V[] {}, new Results(v_154));
v_151 = values_155[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_151, () => (V)(R.Get(v_151, (V)"x") == (V)11.0d)), () => (V)(R.Get(v_151, (V)"y") == (V)8.0d)))));
var values_156 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(R.Get(v_15, (V)"definitions"), (V)"core:room/gallery"), (V)"markers"), (V)"chest"), (V)1.0d)));
V v_157 = values_156[0];
var values_158 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_149, (V)"markers"), (V)"chest"), (V)1.0d)));
V v_159 = values_158[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(v_157, (V)"x") == (V)12.0d), () => (V)(R.Get(v_157, (V)"y") == (V)3.0d)), () => (V)(R.Get(v_159, (V)"x") == (V)9.0d)), () => (V)(R.Get(v_159, (V)"y") == (V)4.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_149, (V)"editable")) == (V)R.Length(R.Get(R.Get(R.Get(v_15, (V)"definitions"), (V)"core:room/gallery"), (V)"editable"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_147, (V)"rows"), (V)3.0d) == (V)"#..#....#"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"same seed reproduces concrete floor and another seed changes it"}, new Results(new V((RuleFunction)(args_160 => {

var values_161 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {}, new Results((V)201.0d))));
V v_162 = values_161[0];
var values_163 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {}, new Results((V)201.0d))));
V v_164 = values_163[0];
var values_165 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {}, new Results((V)202.0d))));
V v_166 = values_165[0];
R.Call(v_3, R.Join(new V[] {v_162}, new Results(v_164)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_162, (V)"derived_seed") == R.Get(v_164, (V)"derived_seed")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_162, (V)"derived_seed") != R.Get(v_166, (V)"derived_seed")))));
var values_167 = R.Join(new V[] {}, new Results((V)true));
V v_168 = values_167[0];
{ double end_171 = R.Num(R.Get(v_162, (V)"height")), step_172 = R.Num((V)1.0d);
for (double i_170 = R.Num((V)1.0d); step_172 > 0 ? i_170 <= end_171 : i_170 >= end_171; i_170 += step_172) {
V v_169 = i_170;
if (R.Truth((V)(R.Get(R.Get(v_162, (V)"rows"), v_169) != R.Get(R.Get(v_166, (V)"rows"), v_169)))) {
var values_173 = R.Join(new V[] {}, new Results((V)false));
v_168 = values_173[0];

break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(v_168)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(R.Get(R.Get(v_162, (V)"graph"), (V)"critical")) >= (V)6.0d), () => (V)(R.Get(R.Get(R.Get(v_162, (V)"graph"), (V)"critical"), (V)1.0d) == (V)1.0d)), () => (V)(R.Get(R.Get(R.Get(v_162, (V)"graph"), (V)"critical"), (V)R.Length(R.Get(R.Get(v_162, (V)"graph"), (V)"critical"))) == (V)12.0d)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"100 generated seeds validate rooms, loops, routes, chests and variety"}, new Results(new V((RuleFunction)(args_174 => {

var values_175 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("os"), (V)"clock"), Results.Empty));
V v_176 = values_175[0];
var values_177 = R.Join(new V[] {(V)0.0d}, new Results((V)0.0d));
V v_178 = values_177[0];
V v_179 = values_177[1];
var values_180 = R.Join(new V[] {R.Table(), R.Table(), R.Table(), R.Table()}, new Results(R.Table()));
V v_181 = values_180[0];
V v_182 = values_180[1];
V v_183 = values_180[2];
V v_184 = values_180[3];
V v_185 = values_180[4];
var values_186 = R.Join(new V[] {R.Get(rt.Global("math"), (V)"huge")}, new Results((V)0.0d));
V v_187 = values_186[0];
V v_188 = values_186[1];
{ double end_191 = R.Num((V)100.0d), step_192 = R.Num((V)1.0d);
for (double i_190 = R.Num((V)1.0d); step_192 > 0 ? i_190 <= end_191 : i_190 >= end_191; i_190 += step_192) {
V v_189 = i_190;
var values_193 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {}, new Results(v_189))));
V v_194 = values_193[0];
var values_195 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"validate"), R.Join(new V[] {}, new Results(v_194))));
V v_196 = values_195[0];
V v_197 = values_195[1];
R.Call(rt.Global("assert"), R.Join(new V[] {v_196}, new Results(R.Concat(R.Or(R.And(R.Get(v_197, (V)1.0d), () => R.Get(R.Get(v_197, (V)1.0d), (V)"kind")), () => (V)"unknown"), R.Concat((V)" seed ", v_189)))));
if (R.Truth(R.Get(R.Get(v_194, (V)"diagnostics"), (V)"fallback"))) {
var values_198 = R.Join(new V[] {}, new Results((V)(v_178 + (V)1.0d)));
v_178 = values_198[0];
}
if (R.Truth((V)((V)R.Length(R.Get(R.Get(v_194, (V)"diagnostics"), (V)"attempts")) > (V)1.0d))) {
var values_199 = R.Join(new V[] {}, new Results((V)(v_179 + (V)1.0d)));
v_179 = values_199[0];
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_194, (V)"rooms")) >= (V)8.0d), () => (V)((V)R.Length(R.Get(v_194, (V)"rooms")) <= (V)12.0d)))));
var values_200 = R.Join(new V[] {}, new Results((V)true));
var obj_201 = v_185;
V key_202 = (V)R.Length(R.Get(R.Get(v_194, (V)"graph"), (V)"critical"));
R.Set(obj_201, key_202, values_200[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)((V)R.Length(R.Get(v_194, (V)"rooms")) - (V)R.Length(R.Get(R.Get(v_194, (V)"graph"), (V)"critical"))) >= (V)2.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_194, (V)"graph"), (V)"edges")) >= (V)R.Length(R.Get(v_194, (V)"rooms"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(R.Get(v_194, (V)"placements"), (V)"chests")) == (V)5.0d), () => (V)((V)R.Length(R.Get(R.Get(v_194, (V)"placements"), (V)"enemies")) >= (V)6.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_194, R.Get(v_194, (V)"start_x")}, new Results(R.Get(v_194, (V)"start_y"))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_194, R.Get(R.Get(v_194, (V)"elite"), (V)"x")}, new Results(R.Get(R.Get(v_194, (V)"elite"), (V)"y"))))));
var values_203 = R.Join(new V[] {R.Table()}, new Results(R.Table()));
V v_204 = values_203[0];
V v_205 = values_203[1];
foreach (var row_206 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"chests"), new Entry((V)2, (V)"enemies")))))))) {
V v_207 = row_206[0];
V v_208 = row_206[1];
foreach (var row_209 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(R.Get(v_194, (V)"placements"), v_208))))))) {
V v_210 = row_209[0];
V v_211 = row_209[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(v_204, R.Get(v_211, (V)"id")))), () => (V)(!R.Truth(R.Get(v_205, R.Call(v_43, R.Join(new V[] {R.Get(v_211, (V)"x")}, new Results(R.Get(v_211, (V)"y")))).First)))))));
var values_212 = R.Join(new V[] {}, new Results((V)true));
var obj_213 = v_204;
V key_214 = R.Get(v_211, (V)"id");
R.Set(obj_213, key_214, values_212[0]);

var values_215 = R.Join(new V[] {}, new Results((V)true));
var obj_216 = v_205;
V key_217 = R.Call(v_43, R.Join(new V[] {R.Get(v_211, (V)"x")}, new Results(R.Get(v_211, (V)"y")))).First;
R.Set(obj_216, key_217, values_215[0]);
}
}
foreach (var row_218 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_194, (V)"rooms"))))))) {
V v_219 = row_218[0];
V v_220 = row_218[1];
var values_221 = R.Join(new V[] {}, new Results((V)true));
var obj_222 = v_183;
V key_223 = R.Get(v_220, (V)"template_id");
R.Set(obj_222, key_223, values_221[0]);
var values_224 = R.Join(new V[] {}, new Results((V)true));
var obj_225 = v_184;
V key_226 = R.Get(v_220, (V)"role");
R.Set(obj_225, key_226, values_224[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(v_220, (V)"markers"), (V)"secret_anchor")) >= (V)1.0d))));
}
foreach (var row_227 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_194, (V)"corridors"))))))) {
V v_228 = row_227[0];
V v_229 = row_227[1];
var values_230 = R.Join(new V[] {}, new Results((V)true));
var obj_231 = v_181;
V key_232 = R.Get(v_229, (V)"form");
R.Set(obj_231, key_232, values_230[0]);
var values_233 = R.Join(new V[] {}, new Results((V)true));
var obj_234 = v_182;
V key_235 = R.Get(v_229, (V)"length");
R.Set(obj_234, key_235, values_233[0]);
var values_236 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"min"), R.Join(new V[] {v_187}, new Results(R.Get(v_229, (V)"length")))));
v_187 = values_236[0];
var values_237 = R.Join(new V[] {}, R.Call(R.Get(rt.Global("math"), (V)"max"), R.Join(new V[] {v_188}, new Results(R.Get(v_229, (V)"length")))));
v_188 = values_237[0];
}
}
}
var values_238 = R.Join(new V[] {}, new Results((V)0.0d));
V v_239 = values_238[0];
foreach (var row_240 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("pairs"), R.Join(new V[] {}, new Results(v_182)))))) {
V v_241 = row_240[0];
var values_242 = R.Join(new V[] {}, new Results((V)(v_239 + (V)1.0d)));
v_239 = values_242[0];
}
foreach (var row_243 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Table(new Entry((V)1, (V)"start"), new Entry((V)2, (V)"combat"), new Entry((V)3, (V)"treasure"), new Entry((V)4, (V)"landmark"), new Entry((V)5, (V)"elite")))))))) {
V v_244 = row_243[0];
V v_245 = row_243[1];
R.Call(rt.Global("assert"), R.Join(new V[] {R.Get(v_184, v_245)}, new Results(R.Concat((V)"missing role ", v_245))));
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_181, (V)"straight"), () => R.Get(v_181, (V)"dogleg")), () => (V)(v_239 >= (V)10.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(v_185, (V)6.0d), () => R.Get(v_185, (V)8.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(v_187 <= (V)7.0d), () => (V)(v_188 >= (V)25.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.Get(v_183, (V)"core:room/arena"), () => R.Get(v_183, (V)"core:room/entry")), () => R.Get(v_183, (V)"core:room/gallery")))));
R.Call(rt.Global("print"), R.Join(new V[] {}, R.Invoke((V)"CORPUS 100 seeds: %d fallback, %d retried, lengths %d\u00e2\u0080\u0093%d, %.3fs CPU", "format", R.Join(new V[] {v_178, v_179, v_187, v_188}, new Results((V)(R.Call(R.Get(rt.Global("os"), (V)"clock"), Results.Empty).First - v_176))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"editable corridor fault repairs exactly; protected fault retries then falls back"}, new Results(new V((RuleFunction)(args_246 => {

var values_247 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"editable"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_248 = values_247[0];
var values_249 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"editable"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_250 = values_249[0];
R.Call(v_3, R.Join(new V[] {v_248}, new Results(v_250)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(R.Get(R.Get(v_248, (V)"diagnostics"), (V)"fallback"))), () => (V)((V)R.Length(R.Get(v_248, (V)"repairs")) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(R.And((V)(R.Get(R.Get(R.Get(v_248, (V)"repairs"), (V)1.0d), (V)"kind") == (V)"restore_corridor"), () => (V)(R.Get(R.Get(R.Get(v_248, (V)"repairs"), (V)1.0d), (V)"x") == (V)23.0d)), () => (V)(R.Get(R.Get(R.Get(v_248, (V)"repairs"), (V)1.0d), (V)"y") == (V)24.0d)), () => R.Get(R.Get(R.Get(v_248, (V)"repairs"), (V)1.0d), (V)"valid_after")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"validate"), R.Join(new V[] {}, new Results(v_248)))));
var values_251 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"protected"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_252 = values_251[0];
var values_253 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {(V)301.0d}, new Results(R.Table(new Entry((V)"fault", (V)"protected"), new Entry((V)"retry_limit", (V)1.0d))))));
V v_254 = values_253[0];
R.Call(v_3, R.Join(new V[] {v_252}, new Results(v_254)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Get(R.Get(v_252, (V)"diagnostics"), (V)"fallback"), () => (V)((V)R.Length(R.Get(R.Get(v_252, (V)"diagnostics"), (V)"attempts")) == (V)2.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(R.Get(R.Get(R.Get(v_252, (V)"diagnostics"), (V)"attempts"), (V)1.0d), (V)"repairs")) == (V)0.0d))));
{ double end_257 = R.Num((V)1.0d), step_258 = R.Num((V)1.0d);
for (double i_256 = R.Num((V)0.0d); step_258 > 0 ? i_256 <= end_257 : i_256 >= end_257; i_256 += step_258) {
V v_255 = i_256;
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Get(v_252, (V)"diagnostics"), (V)"attempts"), (V)(v_255 + (V)1.0d)), (V)"derived_seed") == R.Call(R.Get(v_13, (V)"derive"), R.Join(new V[] {(V)301.0d}, new Results(R.Concat(R.Get(v_11, (V)"floor_id"), R.Concat((V)":", v_255))))).First))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"validate"), R.Join(new V[] {}, new Results(v_252)))));
var values_259 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {}, new Results((V)302.0d))));
V v_260 = values_259[0];
var values_261 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_260, (V)"corridors"), (V)1.0d), (V)"from_socket")));
V v_262 = values_261[0];
R.Call(R.Get(v_11, (V)"set_tile"), R.Join(new V[] {v_260, R.Get(v_262, (V)"x"), R.Get(v_262, (V)"y"), (V)"#"}, new Results((V)"w")));
var values_263 = R.Join(new V[] {}, R.Call(R.Get(v_11, (V)"repair"), R.Join(new V[] {}, new Results(v_260))));
V v_264 = values_263[0];
V v_265 = values_263[1];
V v_266 = values_263[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(!R.Truth(v_264)), () => (V)((V)R.Length(v_265) > (V)0.0d)), () => (V)((V)R.Length(v_266) == (V)0.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(v_260, (V)"rows"), R.Get(v_262, (V)"y")), "sub", R.Join(new V[] {R.Get(v_262, (V)"x")}, new Results(R.Get(v_262, (V)"x")))).First == (V)"#"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"generated runtime has distinct IDs, safe spawn and exact stored map restore"}, new Results(new V((RuleFunction)(args_267 => {

var values_268 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results((V)401.0d))));
V v_269 = values_268[0];
var values_270 = R.Join(new V[] {}, new Results(R.Get(v_269, (V)"generated_map")));
V v_271 = values_270[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_269, (V)"map_id") == R.Get(v_11, (V)"map_id")), () => (V)(R.Get(v_269, (V)"active_floor_id") == R.Get(v_11, (V)"floor_id"))))));
var values_272 = R.Join(new V[] {}, new Results((V)0.0d));
V v_273 = values_272[0];
foreach (var row_274 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"entities"))))))) {
V v_275 = row_274[0];
V v_276 = row_274[1];
if (R.Truth(R.Invoke(R.Get(v_276, (V)"controller"), "match", R.Join(new V[] {}, new Results((V)"^review_"))).First)) {
var values_277 = R.Join(new V[] {}, new Results((V)(v_273 + (V)1.0d)));
v_273 = values_277[0];
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Get(v_269, (V)"entities")) == (V)((V)((V)((V)((V)1.0d + (V)R.Length(R.Get(R.Get(v_271, (V)"placements"), (V)"enemies"))) + (V)R.Length(R.Get(R.Get(v_271, (V)"placements"), (V)"chests"))) + (V)R.Length(R.Get(R.Get(v_271, (V)"placements"), (V)"doors"))) + v_273)))));
var values_278 = R.Join(new V[] {}, new Results(R.Table()));
V v_279 = values_278[0];
foreach (var row_280 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_269, (V)"entities"))))))) {
V v_281 = row_280[0];
V v_282 = row_280[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Get(v_279, R.Get(v_282, (V)"id")))))));

var values_283 = R.Join(new V[] {}, new Results((V)true));
var obj_284 = v_279;
V key_285 = R.Get(v_282, (V)"id");
R.Set(obj_284, key_285, values_283[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_282, (V)"position"), (V)"floor_id") == R.Get(v_271, (V)"floor_id")))));
}
var values_286 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_269))));
V v_287 = values_286[0];
var values_288 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_287))));
V v_289 = values_288[0];
R.Call(v_3, R.Join(new V[] {v_287}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_289)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.Or((V)(R.Get(R.Get(v_289, (V)"generated_map"), (V)"rows") == V.Nil), () => (V)(R.Get(R.Get(R.Get(v_289, (V)"generated_map"), (V)"rows"), (V)1.0d) == R.Get(R.Get(v_271, (V)"rows"), (V)1.0d))))));
var values_290 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_269))));
V v_291 = values_290[0];
var values_292 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_291, (V)"generated_map"), (V)"visual_rows"), R.Get(v_271, (V)"start_y"))));
V v_293 = values_292[0];
var values_294 = R.Join(new V[] {}, new Results(R.Or(R.And((V)(R.Invoke(v_293, "sub", R.Join(new V[] {R.Get(v_271, (V)"start_x")}, new Results(R.Get(v_271, (V)"start_x")))).First == (V)"m"), () => (V)"s"), () => (V)"m")));
V v_295 = values_294[0];
var values_296 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_293, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)(R.Get(v_271, (V)"start_x") - (V)1.0d)))).First, R.Concat(v_295, R.Invoke(v_293, "sub", R.Join(new V[] {}, new Results((V)(R.Get(v_271, (V)"start_x") + (V)1.0d)))).First))));
var obj_297 = R.Get(R.Get(v_291, (V)"generated_map"), (V)"visual_rows");
V key_298 = R.Get(v_271, (V)"start_y");
R.Set(obj_297, key_298, values_296[0]);
var values_299 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, new Results(v_291))));
V v_300 = values_299[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Invoke(R.Get(R.Get(R.Get(v_300, (V)"generated_map"), (V)"visual_rows"), R.Get(v_271, (V)"start_y")), "sub", R.Join(new V[] {R.Get(v_271, (V)"start_x")}, new Results(R.Get(v_271, (V)"start_x")))).First == v_295))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(v_300, (V)"generated_map"), (V)"visual_rows"), R.Get(v_271, (V)"start_y")) != R.Get(R.Get(R.Call(R.Get(v_11, (V)"generate"), R.Join(new V[] {}, new Results((V)401.0d))).First, (V)"visual_rows"), R.Get(v_271, (V)"start_y"))))));
var values_301 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_269))));
V v_302 = values_301[0];
var values_303 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(R.Get(v_302, (V)"generated_map"), (V)"corridors"), (V)1.0d), (V)"from_socket")));
V v_304 = values_303[0];
R.Call(R.Get(v_11, (V)"set_tile"), R.Join(new V[] {R.Get(v_302, (V)"generated_map"), R.Get(v_304, (V)"x"), R.Get(v_304, (V)"y"), (V)"#"}, new Results((V)"w")));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(rt.Global("pcall"), R.Join(new V[] {R.Get(v_9, (V)"restore")}, new Results(v_302))).First)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"world and per-chest loot streams leave combat RNG untouched"}, new Results(new V((RuleFunction)(args_305 => {

var values_306 = R.Join(new V[] {}, new Results((V)402.0d));
V v_307 = values_306[0];
var values_308 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new"), R.Join(new V[] {}, new Results(v_307))));
V v_309 = values_308[0];
var values_310 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results(v_307))));
V v_311 = values_310[0];
var values_312 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results(v_307))));
V v_313 = values_312[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_309, (V)"rng"), (V)"state") == R.Get(R.Get(v_311, (V)"rng"), (V)"state")))));
var values_314 = R.Join(new V[] {}, new Results(R.Get(R.Get(v_311, (V)"rng"), (V)"state")));
V v_315 = values_314[0];
var values_316 = R.Join(new V[] {}, new Results(R.Get(v_311, (V)"generated_map")));
V v_317 = values_316[0];
R.Call(v_47, R.Join(new V[] {}, new Results(v_311)));

R.Call(v_47, R.Join(new V[] {}, new Results(v_313)));
var values_318 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_311))).First, (V)"position")));
V v_319 = values_318[0];
var values_320 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_311, (V)"core:enemy/mossbound_guard", (V)(R.Get(v_319, (V)"x") + (V)1.0d), R.Get(v_319, (V)"y")}, new Results((V)1000.0d))));
V v_321 = values_320[0];
var values_322 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_313, (V)"core:enemy/mossbound_guard", (V)(R.Get(v_319, (V)"x") + (V)1.0d), R.Get(v_319, (V)"y")}, new Results((V)1000.0d))));
V v_323 = values_322[0];
var values_324 = R.Join(new V[] {(V)1.0d}, new Results((V)1.0d));
var obj_325 = v_321;
V key_326 = (V)"hp";
var obj_327 = v_323;
V key_328 = (V)"hp";
R.Set(obj_325, key_326, values_324[0]);
R.Set(obj_327, key_328, values_324[1]);
var values_329 = R.Join(new V[] {}, new Results((V)500000.0d));
var obj_330 = R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_311))).First;
V key_331 = (V)"crit_chance_ppm";
R.Set(obj_330, key_331, values_329[0]);
var values_332 = R.Join(new V[] {}, new Results((V)500000.0d));
var obj_333 = R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_313))).First;
V key_334 = (V)"crit_chance_ppm";
R.Set(obj_333, key_334, values_332[0]);
var values_335 = Results.Empty;
V v_336 = values_335[0];
{ double end_339 = R.Num((V)(R.Get(v_317, (V)"height") - (V)1.0d)), step_340 = R.Num((V)1.0d);
for (double i_338 = R.Num((V)2.0d); step_340 > 0 ? i_338 <= end_339 : i_338 >= end_339; i_338 += step_340) {
V v_337 = i_338;
{ double end_343 = R.Num((V)(R.Get(v_317, (V)"width") - (V)1.0d)), step_344 = R.Num((V)1.0d);
for (double i_342 = R.Num((V)2.0d); step_344 > 0 ? i_342 <= end_343 : i_342 >= end_343; i_342 += step_344) {
V v_341 = i_342;
if (R.Truth(R.And((V)(!R.Truth(v_336)), () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_317, v_341}, new Results(v_337))).First))) {
var values_345 = R.Join(new V[] {}, new Results((V)true));
V v_346 = values_345[0];
foreach (var row_347 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_311, (V)"entities"))))))) {
V v_348 = row_347[0];
V v_349 = row_347[1];
if (R.Truth(R.And(R.And(R.Get(v_349, (V)"blocks_movement"), () => (V)(R.Get(R.Get(v_349, (V)"position"), (V)"x") == v_341)), () => (V)(R.Get(R.Get(v_349, (V)"position"), (V)"y") == v_337)))) {
var values_350 = R.Join(new V[] {}, new Results((V)false));
v_346 = values_350[0];

break;
}
}
if (R.Truth(R.And(v_346, () => (V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_341 - R.Get(v_319, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_337 - R.Get(v_319, (V)"y"))))).First) > (V)20.0d)))) {
var values_351 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_chest"), R.Join(new V[] {v_311, (V)"core:chest/free_elite", v_341, v_337}, new Results((V)"test:extra-chest"))));
v_336 = values_351[0];
}
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(v_336, () => (V)(R.Get(R.Get(v_311, (V)"rng"), (V)"state") == v_315)))));
var values_352 = R.Join(new V[] {}, new Results(R.Get(R.Get(R.Get(v_313, (V)"generated_map"), (V)"visual_rows"), (V)2.0d)));
V v_353 = values_352[0];
var values_354 = R.Join(new V[] {}, new Results(R.Concat(R.Invoke(v_353, "sub", R.Join(new V[] {(V)1.0d}, new Results((V)2.0d))).First, R.Concat((V)"m", R.Invoke(v_353, "sub", R.Join(new V[] {}, new Results((V)4.0d))).First))));
var obj_355 = R.Get(R.Get(v_313, (V)"generated_map"), (V)"visual_rows");
V key_356 = (V)2.0d;
R.Set(obj_355, key_356, values_354[0]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_313, (V)"rng"), (V)"state") == v_315))));
var values_357 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_311}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_358 = values_357[0];
V v_359 = values_357[1];
V v_360 = values_357[2];
var values_361 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_313}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)1.0d), new Entry((V)"dy", (V)0.0d))))));
V v_362 = values_361[0];
V v_363 = values_361[1];
V v_364 = values_361[2];
R.Call(v_3, R.Join(new V[] {v_360}, new Results(v_364)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(v_311, (V)"progression"), (V)"level") == R.Get(R.Get(v_313, (V)"progression"), (V)"level")), () => (V)(R.Get(R.Get(v_311, (V)"rng"), (V)"state") == R.Get(R.Get(v_313, (V)"rng"), (V)"state"))))));
var values_365 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results(v_307))));
V v_366 = values_365[0];
foreach (var row_367 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_311, (V)"entities"))))))) {
V v_368 = row_367[0];
V v_369 = row_367[1];
if (R.Truth(R.And((V)(R.Get(v_369, (V)"controller") == (V)"chest"), () => (V)(R.Get(v_369, (V)"stable_marker_id") != (V)"test:extra-chest")))) {
var values_370 = Results.Empty;
V v_371 = values_370[0];
foreach (var row_372 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_366, (V)"entities"))))))) {
V v_373 = row_372[0];
V v_374 = row_372[1];
if (R.Truth((V)(R.Get(v_374, (V)"stable_marker_id") == R.Get(v_369, (V)"stable_marker_id")))) {
var values_375 = R.Join(new V[] {}, new Results(v_374));
v_371 = values_375[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_371)));
R.Call(v_3, R.Join(new V[] {R.Get(v_369, (V)"offers")}, new Results(R.Get(v_371, (V)"offers"))));
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_366, (V)"rng"), (V)"state") == v_315))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"guard pursues across room boundary; generated walls block firing"}, new Results(new V((RuleFunction)(args_376 => {

var values_377 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_378 = values_377[0];
R.Call(v_47, R.Join(new V[] {}, new Results(v_378)));
var values_379 = Results.Empty;
V v_380 = values_379[0];
foreach (var row_381 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_378, (V)"entities"))))))) {
V v_382 = row_381[0];
V v_383 = row_381[1];
if (R.Truth(R.And(R.And(R.And((V)(R.Get(v_383, (V)"controller") == (V)"ai"), () => (V)(R.Get(v_383, (V)"definition_id") == (V)"core:enemy/mossbound_guard")), () => (V)(R.Get(R.Get(v_383, (V)"position"), (V)"x") > (V)40.0d)), () => (V)(R.Get(R.Get(v_383, (V)"position"), (V)"y") < (V)30.0d)))) {
var values_384 = R.Join(new V[] {}, new Results(v_383));
v_380 = values_384[0];

break;
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_380)));
R.Call(R.Get(v_17, (V)"enqueue"), R.Join(new V[] {v_378}, new Results(R.Table(new Entry((V)"kind", (V)"actor_ready"), new Entry((V)"due", (V)0.0d), new Entry((V)"actor_id", R.Get(v_380, (V)"id")), new Entry((V)"is_player", (V)false)))));
var values_385 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_378))));
V v_386 = values_385[0];
var values_387 = R.Join(new V[] {(V)1000.0d}, new Results((V)1000.0d));
var obj_388 = v_386;
V key_389 = (V)"hp";
var obj_390 = v_386;
V key_391 = (V)"max_hp";
R.Set(obj_388, key_389, values_387[0]);
R.Set(obj_390, key_391, values_387[1]);
var values_392 = R.Join(new V[] {}, new Results((V)false));
V v_393 = values_392[0];
{ double end_396 = R.Num((V)70.0d), step_397 = R.Num((V)1.0d);
for (double i_395 = R.Num((V)1.0d); step_397 > 0 ? i_395 <= end_396 : i_395 >= end_396; i_395 += step_397) {
V v_394 = i_395;
var values_398 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_378}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_399 = values_398[0];
V v_400 = values_398[1];
V v_401 = values_398[2];
foreach (var row_402 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(v_401)))))) {
V v_403 = row_402[0];
V v_404 = row_402[1];
if (R.Truth(R.And(R.And((V)(R.Get(v_404, (V)"kind") == (V)"Moved"), () => (V)(R.Get(v_404, (V)"source_id") == R.Get(v_380, (V)"id"))), () => (V)(R.Get(v_404, (V)"x") < (V)40.0d)))) {
var values_405 = R.Join(new V[] {}, new Results((V)true));
v_393 = values_405[0];
}
}
if (R.Truth(v_393)) {
break;
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {v_393}, new Results((V)"guard never crossed the room corridor")));
var values_406 = R.Join(new V[] {}, new Results(R.Get(v_378, (V)"generated_map")));
V v_407 = values_406[0];
var values_408 = Results.Empty;
V v_409 = values_408[0];
{ double end_412 = R.Num((V)(R.Get(v_407, (V)"height") - (V)2.0d)), step_413 = R.Num((V)1.0d);
for (double i_411 = R.Num((V)3.0d); step_413 > 0 ? i_411 <= end_412 : i_411 >= end_412; i_411 += step_413) {
V v_410 = i_411;
{ double end_416 = R.Num((V)(R.Get(v_407, (V)"width") - (V)2.0d)), step_417 = R.Num((V)1.0d);
for (double i_415 = R.Num((V)3.0d); step_417 > 0 ? i_415 <= end_416 : i_415 >= end_416; i_415 += step_417) {
V v_414 = i_415;
if (R.Truth(R.And(R.And(R.And((V)(!R.Truth(v_409)), () => (V)(R.Invoke(R.Get(R.Get(v_407, (V)"rows"), v_410), "sub", R.Join(new V[] {v_414}, new Results(v_414))).First == (V)"#")), () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_407, (V)(v_414 - (V)1.0d)}, new Results(v_410))).First), () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_407, (V)(v_414 + (V)1.0d)}, new Results(v_410))).First))) {
var values_418 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"x", v_414), new Entry((V)"y", v_410))));
v_409 = values_418[0];
}
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_409)));
var values_419 = R.Join(new V[] {}, new Results(R.Table(new Entry((V)"id", (V)999.0d), new Entry((V)"definition_id", (V)"core:enemy/thornspitter"), new Entry((V)"position", R.Table(new Entry((V)"floor_id", R.Get(v_407, (V)"floor_id")), new Entry((V)"x", (V)(R.Get(v_409, (V)"x") + (V)1.0d)), new Entry((V)"y", R.Get(v_409, (V)"y")))), new Entry((V)"blocks_movement", (V)true))));
V v_420 = values_419[0];
var values_421 = R.Join(new V[] {(V)(R.Get(v_409, (V)"x") - (V)1.0d)}, new Results(R.Get(v_409, (V)"y")));
var obj_422 = R.Get(v_386, (V)"position");
V key_423 = (V)"x";
var obj_424 = R.Get(v_386, (V)"position");
V key_425 = (V)"y";
R.Set(obj_422, key_423, values_421[0]);
R.Set(obj_424, key_425, values_421[1]);
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(!R.Truth(R.Call(R.Get(v_7, (V)"projectile_passable"), R.Join(new V[] {v_407, R.Get(v_409, (V)"x")}, new Results(R.Get(v_409, (V)"y")))).First)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Call(R.Get(v_5, (V)"choose"), R.Join(new V[] {v_378}, new Results(v_420))).First, (V)"kind") != (V)"windup"))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"real generated fight, pickup, paid chest and boon effect continue"}, new Results(new V((RuleFunction)(args_426 => {

var values_427 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results((V)1.0d))));
V v_428 = values_427[0];
R.Call(v_47, R.Join(new V[] {}, new Results(v_428)));
var values_429 = Results.Empty;
V v_430 = values_429[0];
V v_431 = values_429[1];
V v_432 = values_429[2];
V v_433 = values_429[3];
foreach (var row_434 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_428, (V)"entities"))))))) {
V v_435 = row_434[0];
V v_436 = row_434[1];
if (R.Truth(R.And((V)(R.Get(v_436, (V)"controller") == (V)"chest"), () => (V)(!R.Truth(v_432))))) {
foreach (var row_437 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_436, (V)"offers"))))))) {
V v_438 = row_437[0];
V v_439 = row_437[1];
if (R.Truth((V)(R.Get(v_439, (V)"boon_id") == (V)"core:boon/temporal_echo"))) {
var values_440 = R.Join(new V[] {v_436}, new Results(v_438));
v_432 = values_440[0];
v_433 = values_440[1];

break;
}
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_432)));
var values_441 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_428))).First, (V)"position")));
V v_442 = values_441[0];
var values_443 = R.Join(new V[] {R.Get(rt.Global("math"), (V)"huge")}, new Results(R.Get(rt.Global("math"), (V)"huge")));
V v_444 = values_443[0];
V v_445 = values_443[1];
foreach (var row_446 in R.Iterate(R.Join(new V[] {}, R.Call(rt.Global("ipairs"), R.Join(new V[] {}, new Results(R.Get(v_428, (V)"entities"))))))) {
V v_447 = row_446[0];
V v_448 = row_446[1];
if (R.Truth((V)(R.Get(v_448, (V)"controller") == (V)"ai"))) {
var values_449 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_448, (V)"position"), (V)"x") - R.Get(v_442, (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_448, (V)"position"), (V)"y") - R.Get(v_442, (V)"y"))))).First)));
V v_450 = values_449[0];
var values_451 = R.Join(new V[] {}, new Results((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_448, (V)"position"), (V)"x") - R.Get(R.Get(v_432, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(v_448, (V)"position"), (V)"y") - R.Get(R.Get(v_432, (V)"position"), (V)"y"))))).First)));
V v_452 = values_451[0];
if (R.Truth(R.And((V)(R.Get(v_448, (V)"definition_id") == (V)"core:enemy/mossbound_guard"), () => (V)(v_450 < v_444)))) {
var values_453 = R.Join(new V[] {v_448}, new Results(v_450));
v_430 = values_453[0];
v_444 = values_453[1];
} else if (R.Truth(R.And((V)(R.Get(v_448, (V)"definition_id") == (V)"core:enemy/thornspitter"), () => (V)(v_452 < v_445)))) {
var values_454 = R.Join(new V[] {v_448}, new Results(v_452));
v_431 = values_454[0];
v_445 = values_454[1];
}
}
}
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And(v_430, () => v_431), () => v_432))));
V v_455 = V.Nil;
v_455 = new V((RuleFunction)(args_456 => {
V v_457 = args_456[0];
var values_458 = R.Join(new V[] {R.Get(R.Get(v_457, (V)"position"), (V)"x")}, new Results(R.Get(R.Get(v_457, (V)"position"), (V)"y")));
V v_459 = values_458[0];
V v_460 = values_458[1];
R.Call(v_105, R.Join(new V[] {v_428}, new Results(new V((RuleFunction)(args_461 => {
V v_462 = args_461[0];
V v_463 = args_461[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_462 - v_459)))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_463 - v_460)))).First) == (V)1.0d)));
return Results.Empty;
})))));
var values_464 = R.Join(new V[] {}, new Results((V)0.0d));
V v_465 = values_464[0];
while (R.Truth(R.Call(R.Get(v_9, (V)"entity"), R.Join(new V[] {v_428}, new Results(R.Get(v_457, (V)"id")))).First)) {
var values_466 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_428))).First, (V)"position")));
V v_467 = values_466[0];
var values_468 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_428}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)(v_459 - R.Get(v_467, (V)"x"))), new Entry((V)"dy", (V)(v_460 - R.Get(v_467, (V)"y"))))))));
V v_469 = values_468[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(v_469)));

var values_470 = R.Join(new V[] {}, new Results((V)(v_465 + (V)1.0d)));
v_465 = values_470[0];

R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(v_465 <= (V)10.0d))));
}
R.Call(v_105, R.Join(new V[] {v_428}, new Results(new V((RuleFunction)(args_471 => {
V v_472 = args_471[0];
V v_473 = args_471[1];
return R.Join(new V[] {}, new Results(R.And((V)(v_472 == v_459), () => (V)(v_473 == v_460))));
return Results.Empty;
})))));
return Results.Empty;
}));
R.Call(v_455, R.Join(new V[] {}, new Results(v_430)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_428, (V)"currency") == (V)4.0d), () => (V)(R.Get(R.Get(v_428, (V)"progression"), (V)"level") >= (V)2.0d)))));
R.Call(v_455, R.Join(new V[] {}, new Results(v_431)));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(v_428, (V)"currency") == (V)7.0d))));
R.Call(v_105, R.Join(new V[] {v_428}, new Results(new V((RuleFunction)(args_474 => {
V v_475 = args_474[0];
V v_476 = args_474[1];
return R.Join(new V[] {}, new Results((V)((V)(R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_475 - R.Get(R.Get(v_432, (V)"position"), (V)"x"))))).First + R.Call(R.Get(rt.Global("math"), (V)"abs"), R.Join(new V[] {}, new Results((V)(v_476 - R.Get(R.Get(v_432, (V)"position"), (V)"y"))))).First) == (V)1.0d)));
return Results.Empty;
})))));
var values_477 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_428))));
V v_478 = values_477[0];
var values_479 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_428))).First, (V)"position")));
V v_480 = values_479[0];
var values_481 = R.Join(new V[] {(V)(R.Get(R.Get(v_432, (V)"position"), (V)"x") - R.Get(v_480, (V)"x"))}, new Results((V)(R.Get(R.Get(v_432, (V)"position"), (V)"y") - R.Get(v_480, (V)"y"))));
V v_482 = values_481[0];
V v_483 = values_481[1];
var values_484 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_428}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_482), new Entry((V)"dy", v_483))))));
V v_485 = values_484[0];
V v_486 = values_484[1];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(!R.Truth(v_485)), () => (V)(v_486 == (V)"chest")))));
R.Call(v_3, R.Join(new V[] {v_478}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_428)))));
var values_487 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"chest_options"), R.Join(new V[] {v_428}, new Results(R.Get(v_432, (V)"id")))));
V v_488 = values_487[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)((V)R.Length(R.Get(v_488, (V)"offers")) == (V)3.0d), () => (V)(R.Get(R.Get(R.Get(v_488, (V)"offers"), v_433), (V)"boon_id") == (V)"core:boon/temporal_echo")))));
var values_489 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_428}, new Results(R.Table(new Entry((V)"kind", (V)"claim_chest"), new Entry((V)"chest_id", R.Get(v_432, (V)"id")), new Entry((V)"offer_index", v_433))))));
V v_490 = values_489[0];
V v_491 = values_489[1];
V v_492 = values_489[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)(R.Get(v_428, (V)"currency") == (V)2.0d), () => R.Get(v_432, (V)"claimed")), () => (V)((V)R.Length(R.Call(v_29, R.Join(new V[] {v_492}, new Results((V)"BoonGranted"))).First) == (V)1.0d)))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_428}, new Results(R.Table(new Entry((V)"kind", (V)"move"), new Entry((V)"dx", v_482), new Entry((V)"dy", v_483)))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_428))).First, (V)"position"), (V)"x") == R.Get(R.Get(v_432, (V)"position"), (V)"x")), () => (V)(R.Get(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_428))).First, (V)"position"), (V)"y") == R.Get(R.Get(v_432, (V)"position"), (V)"y"))))));
var values_493 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_428}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d)))))));
V v_494 = values_493[0];
V v_495 = values_493[1];
V v_496 = values_493[2];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(v_29, R.Join(new V[] {v_496}, new Results((V)"DelayedEffectScheduled"))).First) >= (V)1.0d))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)(R.Get(R.Get(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_428))).First, (V)"boons"), (V)"core:boon/temporal_echo"), R.Get(R.Get(R.Get(v_488, (V)"offers"), v_433), (V)"rarity")) == (V)1.0d))));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_428))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_428)))))))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"generated snapshot preserves pending windup, echo, IDs and continuation"}, new Results(new V((RuleFunction)(args_497 => {

var values_498 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results((V)403.0d))));
V v_499 = values_498[0];
R.Call(v_47, R.Join(new V[] {}, new Results(v_499)));
var values_500 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_499))).First, (V)"position")));
V v_501 = values_500[0];
var values_502 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"spawn_enemy"), R.Join(new V[] {v_499, (V)"core:enemy/mossbound_guard", (V)(R.Get(v_501, (V)"x") + (V)1.0d)}, new Results(R.Get(v_501, (V)"y")))));
V v_503 = values_502[0];
R.Call(R.Get(v_9, (V)"set_boon_stacks"), R.Join(new V[] {v_499, R.Get(v_499, (V)"player_id"), (V)"core:boon/temporal_echo", (V)"common"}, new Results((V)1.0d)));
R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_499}, new Results(R.Table(new Entry((V)"kind", (V)"basic_attack"), new Entry((V)"dx", (V)0.0d), new Entry((V)"dy", (-(V)1.0d))))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And((V)(R.Get(v_499, (V)"clock") == (V)100.0d), () => R.Get(v_503, (V)"pending_attack")))));
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results((V)((V)R.Length(R.Call(R.Get(v_9, (V)"pending_boon_effects"), R.Join(new V[] {}, new Results(v_499))).First) >= (V)1.0d))));
var values_504 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"restore"), R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_499))))));
V v_505 = values_504[0];
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_499))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_505)))));
var values_506 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_499}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_507 = values_506[0];
V v_508 = values_506[1];
V v_509 = values_506[2];
var values_510 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"submit"), R.Join(new V[] {v_505}, new Results(R.Table(new Entry((V)"kind", (V)"wait"))))));
V v_511 = values_510[0];
V v_512 = values_510[1];
V v_513 = values_510[2];
R.Call(v_3, R.Join(new V[] {v_509}, new Results(v_513)));
R.Call(v_3, R.Join(new V[] {R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_499))).First}, R.Call(R.Get(v_9, (V)"snapshot"), R.Join(new V[] {}, new Results(v_505)))));
return Results.Empty;
})))));
R.Call(v_2, R.Join(new V[] {(V)"Crescent Reach and targeting consult generated collision tiles"}, new Results(new V((RuleFunction)(args_514 => {

var values_515 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"new_generated"), R.Join(new V[] {}, new Results((V)404.0d))));
V v_516 = values_515[0];
var values_517 = R.Join(new V[] {}, new Results(R.Get(R.Call(R.Get(v_9, (V)"player"), R.Join(new V[] {}, new Results(v_516))).First, (V)"position")));
V v_518 = values_517[0];
R.Call(R.Get(v_9, (V)"set_boon_stacks"), R.Join(new V[] {v_516, R.Get(v_516, (V)"player_id"), (V)"core:boon/crescent_reach", (V)"common"}, new Results((V)1.0d)));
var values_519 = R.Join(new V[] {}, R.Call(R.Get(v_9, (V)"effective_ability"), R.Join(new V[] {v_516, (V)"sweeping_slash", (V)0.0d}, new Results((-(V)1.0d)))));
V v_520 = values_519[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.And((V)((V)R.Length(v_520) == (V)4.0d), () => (V)(R.Get(R.Get(v_520, (V)4.0d), (V)"x") == R.Get(v_518, (V)"x"))), () => (V)(R.Get(R.Get(v_520, (V)4.0d), (V)"y") == (V)(R.Get(v_518, (V)"y") - (V)2.0d))))));
var values_521 = R.Join(new V[] {}, new Results(R.Get(v_516, (V)"generated_map")));
V v_522 = values_521[0];
R.Call(rt.Global("assert"), R.Join(new V[] {}, new Results(R.And(R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_522, R.Get(v_518, (V)"x")}, new Results((V)(R.Get(v_518, (V)"y") - (V)1.0d)))).First, () => R.Call(R.Get(v_7, (V)"walkable"), R.Join(new V[] {v_522, R.Get(v_518, (V)"x")}, new Results((V)(R.Get(v_518, (V)"y") - (V)2.0d)))).First))));
return Results.Empty;
})))));
return Results.Empty;
}))));
return Results.Empty;
} }
